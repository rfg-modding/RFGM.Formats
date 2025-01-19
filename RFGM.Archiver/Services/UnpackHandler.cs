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

public class UnpackHandler(SupportedFormats supportedFormats, IFileSystem fileSystem, ImageConverter imageConverter, ILogger<UnpackHandler> log) : HandlerBase<UnpackMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(UnpackMessage message, CancellationToken token)
    {
        log.LogInformation("Reading {name}", message.Archive.Name);
        var extension = fileSystem.Path.GetExtension(message.Archive.FullName).ToLowerInvariant();
        var format = supportedFormats.GuessByExtension(extension);
        if (format == FileFormat.Unsupported)
        {
            log.LogTrace("Unsupported format [{path}]", message.Archive.FullName);
            return [];
        }

        if(!message.Formats.Contains(format))
        {
            log.LogTrace("Supported format, not selected for unpack");
            return [];
        }
        switch (format)
        {
            case FileFormat.Vpp:
            case FileFormat.Str2:
                return await UnpackVpp(message, token);
            case FileFormat.Peg:
                return await UnpackPeg(message, token);
            default:
                throw new ArgumentOutOfRangeException();
        }
        return [];
    }

    private async Task<IEnumerable<IMessage>> UnpackVpp(UnpackMessage message, CancellationToken token)
    {
        var reader = new VppReader();
        await using var stream = message.Archive.OpenRead();
        var hash = message.Hash ? await Utils.ComputeHash(stream) : string.Empty;
        var logicalArchive = await Task.Run(() => reader.Read(stream, message.Archive.Name, token), token);
        var relativePath = message.RelativePath.Append(logicalArchive.Name).ToList();
        var relativePathString = string.Join("/", relativePath);
        var dstName = $"{logicalArchive.Name}.{logicalArchive.Mode.ToString().ToLowerInvariant()}";
        var destination = CreateDirectory(message.OutputPath, dstName);
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(message.FileGlob);
        var entries = logicalArchive.LogicalFiles.ToList();
        Archiver.Metadata.Add(new VppArchive(logicalArchive.Name, relativePathString, logicalArchive.Mode, stream.Length, hash, entries.Count));
        var progress = new ProgressLogger($"Unpacking {relativePathString}", entries.Count, log);
        foreach (var logicalFile in entries)
        {
            token.ThrowIfCancellationRequested();
            if (!matcher.Match(logicalFile.Name).HasMatches)
            {
                log.LogTrace($"Skipped [{logicalFile}]");
                continue;
            }

            await UnpackVppEntry(logicalFile, destination, relativePath, message.Force, message.Hash, token);
            progress.Tick();
        }

        if (message.Recursive)
        {
            throw new NotImplementedException();
        }

        return [];
    }

    private async Task UnpackVppEntry(LogicalFile logicalFile, IDirectoryInfo destination, IReadOnlyList<string> relativePath, bool force, bool hash, CancellationToken token)
    {
        var fileName = $"{logicalFile.Order:D5} {logicalFile.Name}";
        var dstFile = CreateFile(destination, fileName, force);
        var entryHash = hash ? await Utils.ComputeHash(logicalFile.Content) : string.Empty;
        var entryRelativePath = string.Join("/", relativePath.Append(logicalFile.Name));
        log.LogTrace($"Create file [{dstFile}]");
        dstFile.Create().Close();
        await using var dst = dstFile.OpenWrite();
        await logicalFile.Content.CopyToAsync(dst, token);
        log.LogDebug("Unpacked [{path}]", dstFile.FullName);
        Archiver.Metadata.Add(new VppEntry(logicalFile.Name, entryRelativePath, logicalFile.Order, logicalFile.Offset, logicalFile.Content.Length, logicalFile.CompressedSize, entryHash));
    }

    private async Task<IEnumerable<IMessage>> UnpackPeg(UnpackMessage message, CancellationToken token)
    {
        var reader = new PegReader();
        var pegFiles = PairedFiles.FromExistingFile(message.Archive);
        if (pegFiles is null)
        {
            throw new InvalidOperationException($"Could not open CPU+GPU pair of files for [{message.Archive.FullName}]");
        }
        await using var streams = pegFiles.OpenRead();
        var logicalArchive = await Task.Run(() =>reader.Read(streams.Cpu, streams.Gpu, pegFiles.Name, token), token);
        var hash = message.Hash ? await Utils.ComputeHash(streams) : string.Empty;
        var relativePath = message.RelativePath.Append(logicalArchive.Name).ToList();
        var relativePathString = string.Join("/", relativePath);
        var dstName = $"{logicalArchive.Name}";
        var destination = CreateDirectory(message.OutputPath, dstName);
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(message.FileGlob);
        var entries = logicalArchive.LogicalTextures.ToList();
        Archiver.Metadata.Add(new PegArchive(logicalArchive.Name, relativePathString, streams.Size, logicalArchive.Align, hash, entries.Count));
        var progress = new ProgressLogger($"Unpacking {relativePathString}", entries.Count * message.Textures.Count, log);
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
                await UnpackPegEntry(logicalFile, imageFormat, destination, relativePath, message.Force, message.Hash, token);
                progress.Tick();
            }
        }

        if (message.Recursive)
        {
            throw new NotImplementedException();
        }

        return [];
    }

    private async Task UnpackPegEntry(LogicalTexture logicalFile, ImageFormat imageFormat, IDirectoryInfo destination, IReadOnlyList<string> relativePath, bool force, bool hash, CancellationToken token)
    {
        var fileName = logicalFile.BuildConventionName(imageFormat);
        var dstFile = CreateFile(destination, fileName, force);
        var data = await imageConverter.ConvertImage(logicalFile, imageFormat, token);
        var entryHash = hash ? await Utils.ComputeHash(data) : string.Empty;
        var entryRelativePath = string.Join("/", relativePath.Append(logicalFile.Name));
        log.LogTrace($"Create file [{dstFile}]");
        dstFile.Create().Close();
        await using var dst = dstFile.OpenWrite();
        await data.CopyToAsync(dst, token);
        Archiver.Metadata.Add(new PegEntry(logicalFile.Name, entryRelativePath, logicalFile.Order, (uint) logicalFile.DataOffset, logicalFile.Data.Length, logicalFile.Size, logicalFile.Source, logicalFile.AnimTiles, logicalFile.Format, logicalFile.Flags, logicalFile.MipLevels, logicalFile.Align, entryHash));
        log.LogDebug("Unpacked [{path}]", dstFile.FullName);
    }

    private (RfgCpeg.Entry.BitmapFormat format, TextureFlags flags, int mipLevels, string name) ParseFilename(string fileName)
    {
        var match = Constants.TextureNameFormat.Match(fileName.ToLowerInvariant());
        var name = match.Groups["name"].Value + ".tga";
        var formatString = match.Groups["format"].Value;
        var mipLevels = int.Parse(match.Groups["mipLevels"].Value);

        var (format, flags) = formatString switch
        {
            "dxt1" => (RfgCpeg.Entry.BitmapFormat.PcDxt1, TextureFlags.None),
            "dxt1_srgb" => (RfgCpeg.Entry.BitmapFormat.PcDxt1, TextureFlags.Srgb),
            "dxt5" => (RfgCpeg.Entry.BitmapFormat.PcDxt5, TextureFlags.None),
            "dxt5_srgb" => (RfgCpeg.Entry.BitmapFormat.PcDxt5, TextureFlags.Srgb),
            "rgba" => (RfgCpeg.Entry.BitmapFormat.Pc8888, TextureFlags.None),
            "rgba_srgb" => (RfgCpeg.Entry.BitmapFormat.Pc8888, TextureFlags.Srgb),
            _ => throw new ArgumentOutOfRangeException(nameof(formatString), formatString, "Unknown texture format from filename")
        };

        return (format, flags, mipLevels, name);
    }

    private IFileInfo CreateFile(IDirectoryInfo parent, string fileName, bool force)
    {
        var dstFilePath = fileSystem.Path.Combine(parent.FullName, fileName);
        var dstFile = fileSystem.FileInfo.New(dstFilePath);
        if (dstFile.Exists)
        {
            if (force)
            {
                log.LogTrace($"Delete file [{dstFile}]");
                dstFile.Delete();
                dstFile.Refresh();
            }
            else
            {
                throw new InvalidOperationException($"Destination file [{dstFile.FullName}] already exists! Use --force flag to overwrite");
            }
        }

        return dstFile;
    }

    private IDirectoryInfo CreateDirectory(IDirectoryInfo parent, string dirName)
    {
        var path = fileSystem.Path.Combine(parent.FullName, dirName);
        var result = fileSystem.DirectoryInfo.New(path);
        if (!result.Exists)
        {
            log.LogTrace($"Create dir {result}");
            result.Create();
        }

        return result;
    }

}
