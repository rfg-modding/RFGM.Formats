using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Formats;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class CollectMetadataHandler(FormatManager formatManager, ILogger<CollectMetadataHandler> log) : HandlerBase<CollectMetadataMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(CollectMetadataMessage message, CancellationToken token)
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
        // dispose streams
        message.Source.Close();
        message.Secondary?.Close();
        return result;
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
        var relativePath = message.RelativePath.Append(logicalArchive.Name).ToList();
        var relativePathString = string.Join("/", relativePath);
        var entries = logicalArchive.LogicalFiles.ToList();
        Archiver.Metadata.Add(new VppArchive(logicalArchive.Name, relativePathString, logicalArchive.Mode, message.Source.Length, hash, entries.Count));
        var progress = new ProgressLogger($"Reading {relativePathString}", entries.Count, log);
        var result = new List<IMessage>();
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            var item = await ReadVppEntry(logicalFile, entries, relativePath, message.Hash, token);
            if (item != null)
            {
                result.Add(item);
            }
            progress.Tick();
        }
        return result;
    }

    private async Task<CollectMetadataMessage?> ReadVppEntry(LogicalFile logicalFile, IReadOnlyList<LogicalFile> entries, IReadOnlyList<string> relativePath, bool hash, CancellationToken token)
    {
        var ms = new MemoryStream();
        await logicalFile.Content.CopyToAsync(ms, token);
        ms.Seek(0, SeekOrigin.Begin);
        var entryHash = hash ? await Utils.ComputeHash(ms) : string.Empty;
        var entryRelativePath = relativePath.Append(logicalFile.Name).ToList();
        var entryRelativePathString = string.Join("/", entryRelativePath);
        log.LogDebug("Copied [{path}] to memory", logicalFile.Name);
        Archiver.Metadata.Add(new VppEntry(logicalFile.Name, entryRelativePathString, logicalFile.Order, logicalFile.Offset, ms.Length, logicalFile.CompressedSize, entryHash));

        var cpu = PairedFiles.GetCpuFileName(logicalFile.Name);
        var gpu = PairedFiles.GetGpuFileName(logicalFile.Name);
        var gpuEntry = entries.FirstOrDefault(x => x.Name == gpu);
        if (logicalFile.Name == cpu && gpuEntry != null)
        {
            var ms2 = new MemoryStream();
            await gpuEntry.Content.CopyToAsync(ms2, token);
            ms2.Seek(0, SeekOrigin.Begin);
            log.LogDebug("Copied [{path}] to memory as secondary stream", gpuEntry.Name);
            return new CollectMetadataMessage(ms, ms2, logicalFile.Name, relativePath, hash);
        }

        if (logicalFile.Name == gpu)
        {
            log.LogDebug("Skilled [{path}] as secondary file", logicalFile.Name);
            return null;
        }

        return new CollectMetadataMessage(ms, null, logicalFile.Name, relativePath, hash);
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
        var relativePath = message.RelativePath.Append(logicalArchive.Name).ToList();
        var relativePathString = string.Join("/", relativePath);
        var entries = logicalArchive.LogicalTextures.ToList();
        Archiver.Metadata.Add(new PegArchive(logicalArchive.Name, relativePathString, streams.Size, logicalArchive.Align, hash, entries.Count));
        var progress = new ProgressLogger($"Reading {relativePathString}", entries.Count, log);
        var result = new List<IMessage>();
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            var item = await ReadPegEntry(logicalFile, entries, relativePath, message.Hash, token);
            result.Add(item);
            progress.Tick();
        }
        return result;
    }

    private async Task<IMessage> ReadPegEntry(LogicalTexture logicalFile, IReadOnlyList<LogicalTexture> entries,  IReadOnlyList<string> relativePath, bool hash, CancellationToken token)
    {
        var ms = new MemoryStream();
        await logicalFile.Data.CopyToAsync(ms, token);
        ms.Seek(0, SeekOrigin.Begin);
        var entryHash = hash ? await Utils.ComputeHash(ms) : string.Empty;
        var entryRelativePath = relativePath.Append(logicalFile.Name).ToList();
        var entryRelativePathString = string.Join("/", entryRelativePath);
        Archiver.Metadata.Add(new PegEntry(logicalFile.Name, entryRelativePathString, logicalFile.Order, (uint) logicalFile.DataOffset, logicalFile.Data.Length, logicalFile.Size, logicalFile.Source, logicalFile.AnimTiles, logicalFile.Format, logicalFile.Flags, logicalFile.MipLevels, logicalFile.Align, entryHash));

        log.LogDebug("Copied [{path}] to memory", logicalFile.Name);

        var cpu = PairedFiles.GetCpuFileName(logicalFile.Name);
        var gpu = PairedFiles.GetGpuFileName(logicalFile.Name);
        var gpuEntry = entries.FirstOrDefault(x => x.Name == gpu);
        if (logicalFile.Name == cpu && gpuEntry != null)
        {
            var ms2 = new MemoryStream();
            await gpuEntry.Data.CopyToAsync(ms2, token);
            ms2.Seek(0, SeekOrigin.Begin);
            log.LogDebug("Copied [{path}] to memory as secondary stream", gpuEntry.Name);
            return new CollectMetadataMessage(ms, ms2, logicalFile.Name, relativePath, hash);
        }

        return new CollectMetadataMessage(ms, null, logicalFile.Name, relativePath, hash);
    }
}
