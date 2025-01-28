using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Localization;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services.Handlers;

public class PackDirectoryHandler(IFileSystem fileSystem, FileManager fileManager, ImageConverter imageConverter, LocatextReader locatextReader, ILogger<PackDirectoryHandler> log) : HandlerBase<PackDirectoryMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(PackDirectoryMessage message, CancellationToken token)
    {
        var destination = fileSystem.Path.IsPathFullyQualified(message.Destination)
            ? fileSystem.DirectoryInfo.New(message.Destination)
            : fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(message.DirectoryInfo.Parent!.FullName, message.Destination));

        var descriptor = FormatDescriptors.DetermineByFileSystem(message.DirectoryInfo);
        var entryInfo = descriptor.FromFileSystem(message.DirectoryInfo);
        log.LogTrace("Packing {entry} ", entryInfo);
        Archiver.Destinations.Add(destination.FullName);

        var fileResult = await HandleInternal(message, entryInfo, destination, token);

        var result = new List<IMessage>();
        if (message.Settings.Metadata)
        {
            if (fileResult.Primary is not null)
            {
                var fakeInfo = new EntryInfo(fileResult.Primary.Name, descriptor, new Properties());
                result.Add(new BuildMetadataMessage(fakeInfo, Breadcrumbs.Init(), fileResult.Primary.OpenRead()));
                // fake no-op unpack to collect metadata from nested entries
                result.Add(new UnpackMessage(fakeInfo, fileResult.Primary.OpenRead(), fileResult.Secondary?.OpenRead(), Breadcrumbs.Init(), destination, UnpackSettings.Meta));
            }
            if (fileResult.Secondary is not null)
            {
                var secondaryFakeInfo = new EntryInfo(fileResult.Secondary.Name, descriptor, new Properties());
                result.Add(new BuildMetadataMessage(secondaryFakeInfo, Breadcrumbs.Init(), fileResult.Secondary.OpenRead()));
            }

        }

        return result;
    }

    private async Task<FileResult> HandleInternal(PackDirectoryMessage message, EntryInfo entryInfo, IDirectoryInfo destination, CancellationToken token)
    {
        return entryInfo.Descriptor switch
        {
            PegDescriptor => await PackPeg(message, entryInfo, destination, token),
            Str2Descriptor or VppDescriptor => await PackVpp(message, entryInfo, destination, token),
            {IsContainer: false} => await IgnoreNotContainer(message),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<FileResult> PackVpp(PackDirectoryMessage message, EntryInfo containerInfo, IDirectoryInfo destination, CancellationToken token)
    {
        var readEntries = await ProcessVppEntries(message, token);
        var entries = readEntries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name)
            .ToList();

        var duplicates = entries.GroupBy(x => x.Name).Where(g => g.Count() > 1).ToList();
        if (duplicates.Any())
        {
            var sb = new StringBuilder();
            foreach (var d in duplicates)
            {
                sb.AppendLine($"\t{d.First().Name}");
            }
            throw new InvalidOperationException($"Container [{containerInfo.Name}] has duplicate files:\n{sb}");
        }

        var container = new LogicalArchive(entries, containerInfo.Properties.VppMode!.Value, containerInfo.Name);
        using var writer = new VppWriter(container);
        var name = containerInfo.Descriptor.GetEncodeName(containerInfo);
        var file = fileManager.CreateFileRecursive(destination, name, message.Settings.Force);
        await using var f = file.OpenWrite();
        log.LogDebug("Writing [{file}]", file.FullName);
        await writer.WriteAll(f, token);
        foreach (var x in container.LogicalFiles)
        {
            await x.Content.DisposeAsync();
        }

        return new FileResult(file, null);
    }

    private async Task<FileResult> PackPeg(PackDirectoryMessage message, EntryInfo containerInfo, IDirectoryInfo destination, CancellationToken token)
    {
        var entries = await ProcessPegEntries(message, containerInfo, token);
        entries = entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name)
            .ToList();

        var duplicates = entries.GroupBy(x => x.Name).Where(g => g.Count() > 1).ToList();
        if (duplicates.Any())
        {
            var sb = new StringBuilder();
            foreach (var d in duplicates)
            {
                sb.AppendLine($"\t{d.First().Name}");
            }
            throw new InvalidOperationException($"Container [{containerInfo.Name}] has duplicate files:\n{sb}");
        }

        if (entries.Any(x => x.Order == -1) && entries.Any(x => x.Order != -1))
        {
            throw new InvalidOperationException($"Container [{containerInfo.Name}] has files with explicit order prefix and without prefix. Can't pack them together. Manually specify explicit order for all files or remove it");
        }

        if (entries.All(x => x.Order == -1))
        {
            log.LogWarning("Entry order is implicitly derived from file names");
            var i = 0;
            entries = entries
                .Select(x => x with {Order = i++})
                .ToList();
        }

        var container = new LogicalTextureArchive(entries, containerInfo.Name, 0, 0, containerInfo.Properties.PegAlign!);
        using var writer = new PegWriter(container);
        var name = containerInfo.Descriptor.GetEncodeName(containerInfo);
        var cpuFile = fileManager.CreateFileRecursive(destination, PairedFiles.GetCpuFileName(name)!, message.Settings.Force);
        var gpuFile = fileManager.CreateFileRecursive(destination, PairedFiles.GetGpuFileName(name)!, message.Settings.Force);
        var pair = new PairedFiles(cpuFile, gpuFile, containerInfo.Name);

        await using var f = pair.OpenWrite();
        log.LogDebug("Writing [{cpu}] and [{gpu}]", cpuFile.FullName, gpuFile.FullName);
        await writer.WriteAll(f.Cpu, f.Gpu, token);
        foreach (var x in container.LogicalTextures)
        {
            await x.Data.DisposeAsync();
        }

        return new FileResult(cpuFile, gpuFile);
    }

    private async Task<List<LogicalFile>> ProcessVppEntries(PackDirectoryMessage message, CancellationToken token)
    {
        var result = new List<LogicalFile>();
        var files = message.DirectoryInfo.EnumerateFiles().ToList();
        var progress = new ProgressLogger($"Reading directory {message.DirectoryInfo.FullName}", files.Count, log);
        foreach (var file in message.DirectoryInfo.EnumerateFiles())
        {
            token.ThrowIfCancellationRequested();

            var descriptor = FormatDescriptors.DetermineByFileSystem(file);
            var (entry, data) = descriptor switch
            {
                LocatextDescriptor l => await ProcessLocalization(file, l, token),
                TextureDescriptor => throw new InvalidOperationException("Textures must be packed into PEG containers"),
                XmlDescriptor => throw new InvalidOperationException("Do not pack formatted xml"),
                _ => ProcessRegularFile(file)
            };
            result.Add(new LogicalFile(data, entry.Name, entry.Properties.Index!, null, null));
            progress.Tick();
        }

        return result;
    }

    private async Task<ProcessResult> ProcessLocalization(IFileInfo file, LocatextDescriptor locatextDescriptor, CancellationToken token)
    {
        var entry = locatextDescriptor.FromFileSystem(file);
        await using var f = file.OpenRead();
        var locatext = locatextReader.ReadXml(f, entry.Name);
        var ms = new MemoryStream();
        new LocatextWriter().Write(locatext, ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new(entry, ms);
    }

    private ProcessResult ProcessRegularFile(IFileInfo file)
    {
        var entryInfo = FormatDescriptors.RegularFile.FromFileSystem(file);
        if (entryInfo.Name.EndsWith(".asm_pc", StringComparison.OrdinalIgnoreCase))
        {
            log.LogWarning("File [{name}] is an asm_pc asset assembler file. Any changes to sibling files require updating this asm_pc file, which is not supported yet. Be careful with your changes, game may crash!", file.Name);
        }

        return new(entryInfo, file.OpenRead());
    }

    private async Task<List<LogicalTexture>> ProcessPegEntries(PackDirectoryMessage message, EntryInfo containerInfo, CancellationToken token)
    {
        var result = new List<LogicalTexture>();
        var files = message.DirectoryInfo.EnumerateFiles().ToList();
        var progress = new ProgressLogger($"Reading directory {message.DirectoryInfo.FullName}", files.Count, log);
        foreach (var file in message.DirectoryInfo.EnumerateFiles())
        {
            token.ThrowIfCancellationRequested();
            if (FormatDescriptors.DetermineByFileSystem(file) is not TextureDescriptor textureDescriptor)
            {
                log.LogWarning("File [{name}] is not a texture, skipped", file.Name);
                continue;
            }

            var entry = textureDescriptor.FromFileSystem(file);
            await using var f = file.OpenRead();
            var tmp = textureDescriptor.ToTexture(entry);
            var texture = await imageConverter.ImageToTexture(f, entry.Properties.ImgFmt!.Value, tmp, token);
            result.Add(tmp with{Data = texture});
            progress.Tick();
        }

        return result;
    }

    private Task<FileResult> IgnoreNotContainer(PackDirectoryMessage message)
    {
        log.LogTrace("Skipped packing [{path}]: not a container format", message.DirectoryInfo.FullName);
        return Task.FromResult(new FileResult(null, null));
    }

    private record FileResult(IFileInfo? Primary, IFileInfo? Secondary);

    private record ProcessResult(EntryInfo EntryInfo, Stream Data);
}
