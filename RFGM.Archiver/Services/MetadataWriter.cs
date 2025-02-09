using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Db;
using Metadata = RFGM.Archiver.Models.Metadata;

namespace RFGM.Archiver.Services;

public class MetadataWriter(FileManager fileManager, IFileSystem fileSystem, ArchiverState archiverState, ArchiverContext context, ILogger<MetadataWriter> log)
{
    public async Task WriteCsv(CancellationToken token)
    {
        var metadata = archiverState.GetMetadata();
        if (!metadata.Any())
        {
            return;
        }

        var destination = fileSystem.DirectoryInfo.New(AppContext.BaseDirectory);
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

    public async Task WriteSqlite(CancellationToken token)
    {
        var metadata = archiverState.GetMetadata();
        if (!metadata.Any())
        {
            return;
        }

        await context.RebuildTable(context.Metadata, token);
        var dbMetadata = archiverState.GetMetadata().Select(x => new MetadataEntry(x));
        await context.Metadata.AddRangeAsync(dbMetadata, token);
        await context.SaveChangesAsync(token);
        log.LogInformation($"Saved metadata to:\n\t{context.DbPath}");
    }
}
