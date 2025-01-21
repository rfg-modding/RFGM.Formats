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

public class PackHandler(FormatManager formatManager, FileManager fileManager, IFileSystem fileSystem, ImageConverter imageConverter, ILogger<PackHandler> log) : HandlerBase<PackMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(PackMessage message, CancellationToken token)
    {
        var format = formatManager.GuessFormatByExtension(message.Source);
        if (format == FileFormat.Unsupported)
        {
            log.LogTrace("Unsupported format [{path}]", message.Source.FullName);
            return [];
        }

        return format switch
        {
            FileFormat.Vpp or FileFormat.Str2 => await PackVpp(message, token),
            FileFormat.Peg => await PackPeg(message, token),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<IEnumerable<IMessage>> PackVpp(PackMessage message, CancellationToken token)
    {
        var (archiveMode, archiveName) = formatManager.ParseVppInfo(message.Source);
        var breadcrumbs = message.Breadcrumbs.Descend(Utils.GetNameWithoutNumber(archiveName));
        var relativePath = breadcrumbs.ToString();
        var destination = fileManager.CreateDirectory(message.OutputPath);
        var files = message.Source.EnumerateFiles().ToList();
        var progress = new ProgressLogger($"Preparing {relativePath}", files.Count, log);
        var entries = new List<LogicalFile>();
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            var entry = await CreateVppEntry(file, breadcrumbs, message.Hash);
            if (entry != null)
            {
                entries.Add(entry);

            }
            progress.Tick();
        }

        var archive = new LogicalArchive(entries.OrderBy(x => x.Order), archiveMode, archiveName);
        var writer = new VppWriter(archive);
        var output = fileManager.CreateFile(destination, archive.Name, message.Force);
        await using (var s = output.OpenWrite())
        {
            log.LogInformation("Packing {path}", relativePath);
            await writer.WriteAll(s, token);
        }

        await using var readStream = output.OpenRead();
        var hash = message.Hash ? await Utils.ComputeHash(readStream) : string.Empty;
        Archiver.Metadata.Add(new VppArchive(archive.Name, relativePath, archive.Mode, readStream.Length, hash, files.Count));
        log.LogInformation("Packed {path}", relativePath);

        if (message.Recursive)
        {
            throw new NotImplementedException();
        }

        return [];
    }

    private async Task<LogicalFile?> CreateVppEntry(IFileInfo file, Breadcrumbs parentBreadcrumbs, bool doHash)
    {
        if (fileManager.IsIgnored(file))
        {
            return null;
        }
        var (order, name) = formatManager.ParseVppEntryInfo(file);
        var stream = file.OpenRead();
        var hash = doHash ? await Utils.ComputeHash(stream) : string.Empty;
        var result = new LogicalFile(file.OpenRead(), name, order, null, null);
        var breadcrumbs = parentBreadcrumbs.Descend(result.Name);
        Archiver.Metadata.Add(new VppEntry(result.Name, breadcrumbs.ToString(), result.Order, 0, stream.Length, 0, hash));
        return result;
    }

    private async Task<IEnumerable<IMessage>> PackPeg(PackMessage message, CancellationToken token)
    {
        var (archiveAlign, archiveName) = formatManager.ParsePegInfo(message.Source);
        var breadcrumbs = message.Breadcrumbs.Descend(Utils.GetNameWithoutNumber(archiveName));
        var relativePath = breadcrumbs.ToString();
        var destination = fileManager.CreateDirectory(message.OutputPath);
        var files = message.Source.EnumerateFiles().ToList();
        var progress = new ProgressLogger($"Preparing {relativePath}", files.Count, log);
        var entries = new List<LogicalTexture>();
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            var entry = await CreatePegEntry(file, breadcrumbs, archiveAlign, message.Hash, token);
            if (entry != null)
            {
                entries.Add(entry);

            }
            progress.Tick();
        }

        // TODO: align is always 16 or should be specified in filename?
        var archive = new LogicalTextureArchive(entries.OrderBy(x => x.Order), archiveName, 0, 0, archiveAlign);
        var writer = new PegWriter(archive);
        var cpuName = PairedFiles.GetCpuFileName(archive.Name);
        var gpuName = PairedFiles.GetGpuFileName(archive.Name);
        if (cpuName is null || gpuName is null)
        {
            throw new InvalidOperationException($"Can not decide output file names for [{archive.Name}]: cpu=[{cpuName}] gpu=[{gpuName}]");
        }
        log.LogInformation("Packing {path}", relativePath);
        var cpu = fileManager.CreateFile(destination, cpuName, message.Force);
        var gpu = fileManager.CreateFile(destination, gpuName, message.Force);
        var pair = new PairedFiles(cpu, gpu, archive.Name);
        await using (var x = pair.OpenWrite())
        {
            await writer.WriteAll(x.Cpu, x.Gpu, token);
        }

        await using var readStream = pair.OpenRead();
        var hash = message.Hash ? await Utils.ComputeHash(readStream) : string.Empty;
        Archiver.Metadata.Add(new PegArchive(archive.Name, relativePath, readStream.Size, archive.Align, hash, files.Count));
        log.LogInformation("Packed {path}", relativePath);


        if (message.Recursive)
        {
            throw new NotImplementedException();
        }

        return [];
    }

    private async Task<LogicalTexture?> CreatePegEntry(IFileInfo file, Breadcrumbs parentBreadcrumbs, int align, bool doHash, CancellationToken token)
    {
        if (fileManager.IsIgnored(file))
        {
            return null;
        }
        var (order, name, imageFormat, format, flags, mipLevels, size, source) = formatManager.ParsePegEntryInfo(file);
        var stub = new LogicalTexture(size, source, Size.Zero, format, flags, mipLevels, order, name, -1, -1, align, Stream.Null);
        var stream = file.OpenRead();
        var textureData = await imageConverter.ImageToTexture(stream, imageFormat, stub, token);
        var result = stub with {Data = textureData};
        var hash = doHash ? await Utils.ComputeHash(result.Data) : string.Empty;
        var breadcrumbs = parentBreadcrumbs.Descend(result.Name);
        Archiver.Metadata.Add(new VppEntry(result.Name, breadcrumbs.ToString(), result.Order, 0, result.Data.Length, 0, hash));
        return result;
    }
}
