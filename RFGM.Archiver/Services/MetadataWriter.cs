using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;

namespace RFGM.Archiver.Services;

public class MetadataWriter(FileManager fileManager, IFileSystem fileSystem, ArchiverState archiverState, ILogger<MetadataWriter> log)
{
    public async Task Write(CancellationToken token)
    {
        var metadata = archiverState.GetMetadata();
        if (!metadata.Any())
        {
            return;
        }
        var destination = GetMetadataDestination(archiverState.GetDestinations());
        fileManager.CreateDirectoryRecursive(destination);
        var file = fileManager.CreateFileRecursive(destination, Constants.MetadataFile, true);
        await using var s = file.Create();
        await using var sw = new StreamWriter(s);
        var collection = metadata
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Format, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Hash, StringComparer.OrdinalIgnoreCase)
                .ToList()
            ;
        await sw.WriteLineAsync(Metadata.ToCsvHeader());
        foreach (var x in collection)
        {
            token.ThrowIfCancellationRequested();
            await sw.WriteLineAsync(x.ToCsv());
        }
        log.LogInformation($"Saved metadata to:\n\t{file}");
    }

    private IDirectoryInfo GetMetadataDestination(List<IDirectoryInfo> destinations)
    {
        switch (destinations.Count)
        {
            case 0:
                log.LogWarning("No output destinations, saving metadata to working directory");
                return fileSystem.DirectoryInfo.New(fileSystem.Directory.GetCurrentDirectory());
            case 1:
                return destinations.Single();
            default:
                log.LogWarning("Multiple output destinations, saving metadata to first one");
                return destinations.First();
        }
    }

}
