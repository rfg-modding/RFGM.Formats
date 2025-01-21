using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using RFGM.Archiver.Models;
using RFGM.Formats;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class CollectMetadataHandler(FormatManager formatManager, RecyclableMemoryStreamManager streamManager, ILogger<CollectMetadataHandler> log) : HandlerBase<CollectMetadataMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(CollectMetadataMessage message, CancellationToken token)
    {
        try
        {
            log.LogInformation("Reading {name}", message.Name);
            var format = formatManager.GuessFormatByExtension(message.Name);
            if (format == FileFormat.Unsupported)
            {
                log.LogTrace("Unsupported format [{path}]", message.Name);
                return [];
            }

            var result = format switch
            {
                FileFormat.Vpp or FileFormat.Str2 => await ReadVpp(message, token),
                FileFormat.Peg => await ReadPeg(message, token),
                _ => throw new ArgumentOutOfRangeException()
            };
            return result;
        }
        finally
        {
            await CloseStream(message.Source);
            await CloseStream(message.Secondary);
        }


    }

    private async Task CloseStream(Stream? s)
    {
        if (s is null)
        {
            return;
        }
        await s.DisposeAsync();
    }

    private async Task<IEnumerable<IMessage>> ReadVpp(CollectMetadataMessage message, CancellationToken token)
    {
        if (message.Secondary != null)
        {
            throw new InvalidOperationException($"Unexpected secondary stream");
        }

        var reader = new VppReader();
        var hash = message.Hash ? await Utils.ComputeHash(message.Source) : string.Empty;
        var logicalArchive = await Task.Run(() => reader.Read(message.Source, message.Name, token), token);
        var entries = logicalArchive.LogicalFiles.ToList();
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        Archiver.Metadata.Add(new VppArchive(logicalArchive.Name, relativePath, logicalArchive.Mode, message.Source.Length, hash, entries.Count));
        var progress = new ProgressLogger($"Reading {relativePath}", entries.Count, log);
        var result = new List<IMessage>();
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            var item = await ReadVppEntry(logicalFile, entries, breadcrumbs, message.Hash, token);
            if (item != null)
            {
                result.Add(item);
            }
            progress.Tick();
        }
        return result;
    }

    private async Task<CollectMetadataMessage?> ReadVppEntry(LogicalFile logicalFile, IReadOnlyList<LogicalFile> entries, Breadcrumbs parentBreadcrumbs, bool hash, CancellationToken token)
    {
        var breadcrumbs = parentBreadcrumbs.Descend(logicalFile.Name);
        var relativePath = breadcrumbs.ToString();
        var ms = streamManager.GetStream($"{relativePath}_{Guid.NewGuid()}");
        await logicalFile.Content.CopyToAsync(ms, token);
        ms.Seek(0, SeekOrigin.Begin);
        var entryHash = hash ? await Utils.ComputeHash(ms) : string.Empty;
        log.LogDebug("Copied [{path}] to memory", logicalFile.Name);
        Archiver.Metadata.Add(new VppEntry(logicalFile.Name, relativePath, logicalFile.Order, logicalFile.Offset, ms.Length, logicalFile.CompressedSize, entryHash));

        var cpu = PairedFiles.GetCpuFileName(logicalFile.Name);
        var gpu = PairedFiles.GetGpuFileName(logicalFile.Name);
        var gpuEntry = entries.Skip(logicalFile.Order).FirstOrDefault(x => x.Name == gpu);
        if (logicalFile.Name == cpu && gpuEntry != null)
        {
            var tag = $"{parentBreadcrumbs.Descend(gpuEntry.Name)}_secondary_{Guid.NewGuid()}";
            var ms2 = streamManager.GetStream(tag);
            await gpuEntry.Content.CopyToAsync(ms2, token);
            ms2.Seek(0, SeekOrigin.Begin);
            log.LogDebug("Copied [{path}] to memory as secondary stream", gpuEntry.Name);
            return new CollectMetadataMessage(ms, ms2, logicalFile.Name, parentBreadcrumbs, hash);
        }

        if (logicalFile.Name == gpu)
        {
            // avoid processing secondary file as container
            log.LogDebug("Skipped [{path}] as secondary file", logicalFile.Name);
            await ms.DisposeAsync();
            return null;
        }

        return new CollectMetadataMessage(ms, null, logicalFile.Name, parentBreadcrumbs, hash);
    }

    private async Task<IEnumerable<IMessage>> ReadPeg(CollectMetadataMessage message, CancellationToken token)
    {
        if (message.Secondary == null)
        {
            throw new InvalidOperationException($"Missing secondary stream");
        }

        var reader = new PegReader();
        var streams = new PairedStreams(message.Source, message.Secondary);
        var hash = message.Hash ? await Utils.ComputeHash(streams) : string.Empty;
        var logicalArchive = await Task.Run(() =>reader.Read(streams.Cpu, streams.Gpu, message.Name, token), token);
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        var entries = logicalArchive.LogicalTextures.ToList();
        Archiver.Metadata.Add(new PegArchive(logicalArchive.Name, relativePath, streams.Size, logicalArchive.Align, hash, entries.Count));
        var progress = new ProgressLogger($"Reading {relativePath}", entries.Count, log);
        var result = new List<IMessage>();
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            var item = await ReadPegEntry(logicalFile, breadcrumbs, message.Hash, token);
            result.Add(item);
            progress.Tick();
        }
        return result;
    }

    private async Task<IMessage> ReadPegEntry(LogicalTexture logicalFile, Breadcrumbs parentBreadcrumbs, bool hash, CancellationToken token)
    {
        var breadcrumbs = parentBreadcrumbs.Descend(logicalFile.Name);
        var relativePath = breadcrumbs.ToString();
        var ms = streamManager.GetStream($"{relativePath}_{Guid.NewGuid()}");
        await logicalFile.Data.CopyToAsync(ms, token);
        ms.Seek(0, SeekOrigin.Begin);
        var entryHash = hash ? await Utils.ComputeHash(ms) : string.Empty;
        Archiver.Metadata.Add(new PegEntry(logicalFile.Name, relativePath, logicalFile.Order, (uint) logicalFile.DataOffset, logicalFile.Data.Length, logicalFile.Size, logicalFile.Source, logicalFile.AnimTiles, logicalFile.Format, logicalFile.Flags, logicalFile.MipLevels, logicalFile.Align, entryHash));
        log.LogDebug("Copied [{path}] to memory", logicalFile.Name);
        // peg entries can't contain nested containers or pairs
        return new CollectMetadataMessage(ms, null, logicalFile.Name, parentBreadcrumbs, hash);
    }
}
