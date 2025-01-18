using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class UnpackHandler(SupportedFormats supportedFormats, IFileSystem fileSystem, ILogger<UnpackHandler> log) : HandlerBase<UnpackMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(UnpackMessage message, CancellationToken token)
    {
        log.LogDebug("unpack: {message}", message);
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
                break;
            case FileFormat.Peg:
                //return UnpackPeg(message);
                break;
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
        var vppRelativePath = string.Join("/", message.RelativePath.Append(logicalArchive.Name));
        var dstName = $"{logicalArchive.Name}.{logicalArchive.Mode.ToString().ToLowerInvariant()}";
        var dstPath = fileSystem.Path.Combine(message.OutputPath.FullName, dstName);
        var destination = fileSystem.DirectoryInfo.New(dstPath);
        if (!destination.Exists)
        {
            log.LogTrace($"Create dir {destination}");
            destination.Create();
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(message.FileGlob);
        var count = 0;
        var entryMeta = new List<VppEntry>();
        foreach (var logicalFile in logicalArchive.LogicalFiles)
        {
            count++;
            if (!matcher.Match(logicalFile.Name).HasMatches)
            {
                log.LogTrace($"Skipped [{logicalFile}]");
                continue;
            }

            var fileName = $"{logicalFile.Order:D5} {logicalFile.Name}";
            var dstFilePath = fileSystem.Path.Combine(destination.FullName, fileName);
            var dstFile = fileSystem.FileInfo.New(dstFilePath);
            if (dstFile.Exists)
            {
                if (message.Force)
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

            var entryHash = message.Hash ? await Utils.ComputeHash(logicalFile.Content) : string.Empty;
            var entryRelativePath = string.Join("/", message.RelativePath.Append(logicalArchive.Name).Append(logicalFile.Name));
            log.LogTrace($"Create file [{dstFile}]");
            dstFile.Create().Close();
            await using var dst = dstFile.OpenWrite();
            await logicalFile.Content.CopyToAsync(dst, token);
            log.LogInformation("Unpacked [{path}]", dstFile.FullName);
            entryMeta.Add(new VppEntry(logicalFile.Name, entryRelativePath, logicalFile.Order, logicalFile.Offset, logicalFile.Content.Length, logicalFile.CompressedSize, entryHash));
        }

        var metadata = new VppArchive(logicalArchive.Name, vppRelativePath, logicalArchive.Mode.ToString(), stream.Length, hash, count);
        Archiver.Metadata.Add(metadata);
        foreach (var x in entryMeta)
        {
            Archiver.Metadata.Add(x);
        }

        if (message.Recursive)
        {
            throw new NotImplementedException();
        }

        return [];
    }
}
