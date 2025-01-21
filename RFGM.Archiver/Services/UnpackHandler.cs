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

public class UnpackHandler(FormatManager formatManager, FileManager fileManager, IFileSystem fileSystem, ImageConverter imageConverter, ILogger<UnpackHandler> log) : HandlerBase<UnpackMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(UnpackMessage message, CancellationToken token)
    {
        log.LogInformation("Unpacking {name}", message.Source.Name);
        var format = formatManager.GuessFormatByExtension(message.Source);
        if (format == FileFormat.Unsupported)
        {
            log.LogTrace("Unsupported format [{path}]", message.Source.FullName);
            return [];
        }

        if(!message.Formats.Contains(format))
        {
            log.LogTrace("Supported format, not selected for unpack");
            return [];
        }

        return format switch
        {
            FileFormat.Vpp or FileFormat.Str2 => await UnpackVpp(message, token),
            FileFormat.Peg => await UnpackPeg(message, token),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<IEnumerable<IMessage>> UnpackVpp(UnpackMessage message, CancellationToken token)
    {
        var reader = new VppReader();
        await using var stream = message.Source.OpenRead();
        var hash = message.Hash ? await Utils.ComputeHash(stream) : string.Empty;
        var logicalArchive = await Task.Run(() => reader.Read(stream, message.Source.Name, token), token);
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        var destination = fileManager.CreateSubDirectory(message.OutputPath, logicalArchive.UnpackName);
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(message.FileGlob);
        var entries = logicalArchive.LogicalFiles.ToList();
        Archiver.Metadata.Add(new VppArchive(logicalArchive.Name, relativePath, logicalArchive.Mode, stream.Length, hash, entries.Count));
        var progress = new ProgressLogger($"Unpacking {relativePath}", entries.Count, log);
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            if (!matcher.Match(logicalFile.Name).HasMatches)
            {
                log.LogTrace($"Skipped [{logicalFile}]");
                continue;
            }

            await UnpackVppEntry(logicalFile, destination, breadcrumbs, message.Force, message.Hash, token);
            progress.Tick();
        }

        if (message.Recursive)
        {
            throw new NotImplementedException();
        }

        return [];
    }

    private async Task UnpackVppEntry(LogicalFile logicalFile, IDirectoryInfo destination, Breadcrumbs parentBreadcrumbs, bool force, bool hash, CancellationToken token)
    {
        var dstFile = fileManager.CreateFile(destination, logicalFile.UnpackName, force);
        var entryHash = hash ? await Utils.ComputeHash(logicalFile.Content) : string.Empty;
        var breadcrumbs = parentBreadcrumbs.Descend(logicalFile.Name);
        log.LogTrace($"Create file [{dstFile}]");
        dstFile.Create().Close();
        await using var dst = dstFile.OpenWrite();
        await logicalFile.Content.CopyToAsync(dst, token);
        Archiver.Metadata.Add(new VppEntry(logicalFile.Name, breadcrumbs.ToString(), logicalFile.Order, logicalFile.Offset, logicalFile.Content.Length, logicalFile.CompressedSize, entryHash));
        log.LogDebug("Unpacked [{path}]", dstFile.FullName);
    }

    private async Task<IEnumerable<IMessage>> UnpackPeg(UnpackMessage message, CancellationToken token)
    {
        var reader = new PegReader();
        var pegFiles = PairedFiles.FromExistingFile(message.Source);
        if (pegFiles is null)
        {
            throw new InvalidOperationException($"Could not open CPU+GPU pair of files for [{message.Source.FullName}]");
        }
        await using var streams = pegFiles.OpenRead();
        var logicalArchive = await Task.Run(() =>reader.Read(streams.Cpu, streams.Gpu, pegFiles.Name, token), token);
        var hash = message.Hash ? await Utils.ComputeHash(streams) : string.Empty;
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        var destination = fileManager.CreateSubDirectory(message.OutputPath, logicalArchive.UnpackName);
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(message.FileGlob);
        var entries = logicalArchive.LogicalTextures.ToList();
        Archiver.Metadata.Add(new PegArchive(logicalArchive.Name, relativePath, streams.Size, logicalArchive.Align, hash, entries.Count));
        var progress = new ProgressLogger($"Unpacking {relativePath}", entries.Count * message.Textures.Count, log);
        foreach (var logicalFile in entries)
        {
            if (!matcher.Match(logicalFile.Name).HasMatches)
            {
                log.LogTrace($"Skipped [{logicalFile}]");
                continue;
            }

            foreach (var imageFormat in message.Textures)
            {
                token.ThrowIfCancellationRequested();
                await UnpackPegEntry(logicalFile, imageFormat, destination, breadcrumbs, message.Force, message.Hash, token);
                progress.Tick();
            }
        }

        if (message.Recursive)
        {
            throw new NotImplementedException();
        }

        return [];
    }

    private async Task UnpackPegEntry(LogicalTexture logicalFile, ImageFormat imageFormat, IDirectoryInfo destination, Breadcrumbs parentBreadcrumbs, bool force, bool hash, CancellationToken token)
    {
        var dstFile = fileManager.CreateFile(destination, logicalFile.UnpackName(imageFormat), force);
        var data = await imageConverter.TextureToImage(logicalFile, imageFormat, token);
        var entryHash = hash ? await Utils.ComputeHash(data) : string.Empty;
        var breadcrumbs = parentBreadcrumbs.Descend(logicalFile.Name);
        log.LogTrace($"Create file [{dstFile}]");
        dstFile.Create().Close();
        await using var dst = dstFile.OpenWrite();
        await data.CopyToAsync(dst, token);
        Archiver.Metadata.Add(new PegEntry(logicalFile.Name, breadcrumbs.ToString(), logicalFile.Order, (uint) logicalFile.DataOffset, logicalFile.Data.Length, logicalFile.Size, logicalFile.Source, logicalFile.AnimTiles, logicalFile.Format, logicalFile.Flags, logicalFile.MipLevels, logicalFile.Align, entryHash));
        log.LogDebug("Unpacked [{path}]", dstFile.FullName);
    }
}
