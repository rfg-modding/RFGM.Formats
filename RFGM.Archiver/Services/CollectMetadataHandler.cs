using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Archiver.Models.Metadata;
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
            log.LogInformation("Collecting {name} (secondary present = {})", message.Name, message.Secondary is not null);
            ArgumentOutOfRangeException.ThrowIfNotEqual(message.Source.Position, 0);
            ArgumentOutOfRangeException.ThrowIfNotEqual(message.Secondary?.Position ?? 0, 0);
            var format = formatManager.GuessFormatByExtension(message.Name);
            if (format == FileFormat.Unsupported)
            {
                log.LogTrace("Unsupported format [{path}]", message.Name);
                return [];
            }

            if (FormatManager.PairedFormats.Contains(format) && message.Secondary is null)
            {
                log.LogTrace("Skip [{path}] without secondary stream", message.Name);
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

        var reader = new VppReader(message.OptimizeFor);
        var logicalArchive = await Task.Run(() => reader.Read(message.Source, message.Name, token), token);
        var entries = logicalArchive.LogicalFiles.ToList();
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        var result = new List<IMessage>();
        result.Add(new BuildMetadataMessage(logicalArchive, message.Breadcrumbs, Utils.MakeDeepOwnCopy(message.Source), entries.Count, message.Hash));
        var progress = new ProgressLogger($"Reading {relativePath}", entries.Count, log);
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            result.Add(new BuildMetadataMessage(logicalFile, breadcrumbs, Utils.MakeDeepOwnCopy(logicalFile.Content), 0, message.Hash));
            result.Add(new CollectMetadataMessage(Utils.MakeDeepOwnCopy(logicalFile.Content), null, logicalFile.Name, breadcrumbs, message.Hash, message.OptimizeFor));
            var secondary = LocateSecondary(logicalFile, entries);
            if (secondary != null)
            {
                result.Add(new CollectMetadataMessage(Utils.MakeDeepOwnCopy(logicalFile.Content), Utils.MakeDeepOwnCopy(secondary.Content), logicalFile.Name, breadcrumbs, message.Hash, message.OptimizeFor));
            }

            progress.Tick();
        }
        return result;
    }

    private LogicalFile? LocateSecondary(LogicalFile logicalFile, IReadOnlyList<LogicalFile> entries)
    {
        var cpu = PairedFiles.GetCpuFileName(logicalFile.Name);
        var gpu = PairedFiles.GetGpuFileName(logicalFile.Name);
        var gpuEntry = entries.Skip(logicalFile.Order).FirstOrDefault(x => x.Name == gpu);
        if (logicalFile.Name == cpu && gpuEntry != null)
        {
            return gpuEntry;
        }

        return null;
    }

    private async Task<IEnumerable<IMessage>> ReadPeg(CollectMetadataMessage message, CancellationToken token)
    {
        if (message.Secondary == null)
        {
            throw new InvalidOperationException($"Missing secondary stream");
        }

        var reader = new PegReader();
        var streams = new PairedStreams(message.Source, message.Secondary);
        var logicalArchive = await Task.Run(() =>reader.Read(streams.Cpu, streams.Gpu, message.Name, token), token);
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        var entries = logicalArchive.LogicalTextures.ToList();
        var result = new List<IMessage>();
        result.Add(new BuildMetadataMessage(logicalArchive, message.Breadcrumbs, Utils.MakeDeepOwnCopy(streams), entries.Count, message.Hash));
        var progress = new ProgressLogger($"Reading {relativePath}", entries.Count, log);
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            // peg entries can't contain nested containers or pairs
            result.Add(new CollectMetadataMessage(Utils.MakeDeepOwnCopy(logicalFile.Data), null, logicalFile.Name, breadcrumbs, message.Hash, message.OptimizeFor));
            result.Add(new BuildMetadataMessage(logicalFile, breadcrumbs, Utils.MakeDeepOwnCopy(logicalFile.Data), 0, message.Hash));
            progress.Tick();
        }
        return result;
    }

}
