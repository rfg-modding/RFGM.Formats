using System.Collections.Concurrent;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Formats;

namespace RFGM.Archiver.Services;

public class Archiver(IFileSystem fileSystem, Worker worker, ILogger<Archiver> log)
{
    /// <summary>
    /// Pack or unpack items
    /// </summary>
    public async Task ProcessInput(IReadOnlyList<string> input, CancellationToken token)
    {
        var files = input
                .Where(x => fileSystem.File.Exists(x))
                .Select(x => fileSystem.FileInfo.New(x))
                .Select(x => UnpackMessage.Default(x, fileSystem))
                .ToList()
            ;
        var dirs = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Select(x => fileSystem.DirectoryInfo.New(x))
                .Select(x => PackMessage.Default(x, fileSystem))
                .ToList()
            ;
        if (files.Any() && dirs.Any())
        {
            throw new InvalidOperationException("You specified a mix of files (to unpack) and folders (to pack) at the same time. This is confusing, please do pack and unpack separately");
        }

        if (!files.Any() && !dirs.Any())
        {
            log.LogInformation("Nothing to do: no existing files or folders specified");
            return;

        }

        if (files.Any())
        {
            log.LogInformation("Unpacking files with default settings: {files}", files.Select(x => x.Archive.FullName));
            await worker.Start(files, token);
        }

        if (dirs.Any())
        {
            log.LogInformation("Packing dirs with default settings: {dirs}", dirs.Select(x => x.Path.FullName));
            await worker.Start(dirs, token);
        }

        if (Metadata.Any())
        {
            var outputDirs = files.Select(x => x.OutputPath.FullName).Concat(dirs.Select(x => x.OutputPath.FullName)).Distinct().ToList();
            if (outputDirs.Count > 1)
            {
                log.LogWarning("Multiple output dirs! Saving metadata to first one");
            }

            var dir = outputDirs.First();
            await WriteMetadata(token, dir);
        }

    }

    private async Task WriteMetadata(CancellationToken token, string dir)
    {
        // TODO: better format for metadta, eg full entry relative path and single line per object
        var file = fileSystem.FileInfo.New(Path.Combine(dir, Constants.MetadataFile));
        if (file.Exists)
        {
            log.LogTrace($"Delete file [{file}]");
            file.Delete();
            file.Refresh();
        }
        await using var s = file.Create();
        await using var sw = new StreamWriter(s);
        //await fileSystem.File.WriteAllTextAsync(file.FullName, JsonSerializer.Serialize(Metadata, new JsonSerializerOptions(){WriteIndented = true}), token);
        foreach (var x in Metadata.OrderBy(x => x.RelativePath))
        {
            await sw.WriteLineAsync(x.ToString());
        }
        log.LogInformation($"Saved metadata file [{file}]");
    }

    public static readonly ConcurrentBag<IMetadata> Metadata = new();
}
