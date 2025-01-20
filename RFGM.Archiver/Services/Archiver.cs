using System.Collections.Concurrent;
using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Formats;
using RFGM.Formats.Streams;

namespace RFGM.Archiver.Services;

public class Archiver(IFileSystem fileSystem, FileManager fileManager, Worker worker, ILogger<Archiver> log)
{
    /// <summary>
    /// Pack or unpack items
    /// </summary>
    public async Task ProcessDefault(IReadOnlyList<string> input, CancellationToken token)
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
            log.LogInformation("Unpacking files with default settings: {files}", files.Select(x => x.Source.FullName));
            await worker.Start(files, Environment.ProcessorCount, token);
        }

        if (dirs.Any())
        {
            log.LogInformation("Packing dirs with default settings: {dirs}", dirs.Select(x => x.Source.FullName));
            await worker.Start(dirs, Environment.ProcessorCount, token);
        }

        if (worker.Failed.Any())
        {
            throw new InvalidOperationException($"Failed {worker.Failed.Count} tasks. Check logs for details");
        }

        if (Metadata.Any())
        {
            var outputDirs = files.Select(x => x.OutputPath.FullName).Concat(dirs.Select(x => x.OutputPath.FullName)).Distinct().ToList();
            if (outputDirs.Count > 1)
            {
                log.LogWarning("Multiple output dirs! Saving metadata to first one");
            }

            await WriteMetadata(token, fileSystem.DirectoryInfo.New(outputDirs.First()));
        }

    }

    public async Task CollectMetadata(List<string> input, bool hash, int parallel, CancellationToken token)
    {
        var files = input
            .Where(x => fileSystem.File.Exists(x))
            .Select(x => fileSystem.FileInfo.New(x))
            .ToList();
        var messages1 = FilesToMessages(files, hash);
        var filesFromDirs = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Select(x => fileSystem.DirectoryInfo.New(x))
                .SelectMany(x => x.EnumerateFiles())
                .ToList();
        var messages2 = FilesToMessages(filesFromDirs, hash);
        var all = messages1.Concat(messages2).ToList();
        log.LogInformation("Collecting metadata for: {all}", all.Select(x => x.Name));
        await worker.Start(all, parallel, token);

        if (worker.Failed.Any())
        {
            throw new InvalidOperationException($"Failed {worker.Failed.Count} tasks. Check logs for details");
        }

        if (Metadata.Any())
        {
            var outputDirs = files.Concat(filesFromDirs).Select(x => x.Directory!.FullName).Distinct().ToList();
            if (outputDirs.Count > 1)
            {
                log.LogWarning("Multiple output dirs! Saving metadata to first one");
            }

            await WriteMetadata(token, fileSystem.DirectoryInfo.New(outputDirs.First()));
        }
    }

    private IEnumerable<CollectMetadataMessage> FilesToMessages(IReadOnlyList<IFileInfo> files, bool hash)
    {
        foreach (var file in files)
        {
            var maybePair = PairedFiles.FromExistingFile(file);
            // ignore pairs built from secondary files to avoid duplicates
            if (maybePair != null && maybePair.Gpu.FullName == file.FullName)
            {
                log.LogTrace($"Skipped secondary file [{file.FullName}]");
                continue;
            }

            var primary = maybePair?.Cpu ?? file;
            var secondary = maybePair?.Gpu;
            var name = maybePair?.Name ?? file.Name;
            yield return new CollectMetadataMessage(primary.OpenRead(), secondary?.OpenRead(), Utils.GetNameWithoutNumber(name), [], hash);
        }
    }

    private async Task WriteMetadata(CancellationToken token, IDirectoryInfo destination)
    {
        var file = fileManager.CreateFile(destination, Constants.MetadataFile, true);
        await using var s = file.Create();
        await using var sw = new StreamWriter(s);
        foreach (var x in Metadata.OrderBy(x => x.RelativePath))
        {
            await sw.WriteLineAsync(x.ToString());
        }
        log.LogInformation($"Saved metadata file [{file}]");
    }

    public static readonly ConcurrentBag<IMetadata> Metadata = new();
}
