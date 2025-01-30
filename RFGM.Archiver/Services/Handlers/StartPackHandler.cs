using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Abstractions.Descriptors;
using RFGM.Formats.Localization;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services.Handlers;

public class StartPackHandler(IFileSystem fileSystem, FileManager fileManager, ImageConverter imageConverter, LocatextReader locatextReader, ArchiverState archiverState, ILogger<StartPackHandler> log) : HandlerBase<StartPackMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(StartPackMessage message, CancellationToken token)
    {
        var destination = fileSystem.Path.IsPathFullyQualified(message.Destination)
            ? fileSystem.DirectoryInfo.New(message.Destination)
            // TODO if it's a file, does fake NewDirectory.Parent return parent?
            : fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(fileSystem.DirectoryInfo.New(message.FileSystemInfo.FullName).Parent!.FullName, message.Destination));

        var descriptor = FormatDescriptors.MatchForEncoding(message.FileSystemInfo.Name);
        log.LogTrace("Packing {name} as {descriptor}", message.FileSystemInfo.Name, descriptor.Name);
        var entryInfo = descriptor.ReadEntryForEncoding(message.FileSystemInfo);
        archiverState.RememberDestination(destination.FullName);
        var fileResult = await HandleInternal(message, entryInfo, destination, token);
        var result = new List<IMessage>();
        if (message.Settings.Metadata)
        {
            if (fileResult.Primary is not null)
            {
                var fakeInfo = new EntryInfo(fileResult.Primary.Name, descriptor, new Properties());
                result.Add(new CollectMetadataMessage(fakeInfo, Breadcrumbs.Init(), fileResult.Primary.OpenRead()));
                // fake no-op unpack to collect metadata from nested entries
                result.Add(new UnpackMessage(fakeInfo, fileResult.Primary.OpenRead(), fileResult.Secondary?.OpenRead(), Breadcrumbs.Init(), destination, UnpackSettings.Meta));
            }
            if (fileResult.Secondary is not null)
            {
                var secondaryFakeInfo = new EntryInfo(fileResult.Secondary.Name, descriptor, new Properties());
                result.Add(new CollectMetadataMessage(secondaryFakeInfo, Breadcrumbs.Init(), fileResult.Secondary.OpenRead()));
            }

        }

        return result;
    }

    private async Task<FileResult> HandleInternal(StartPackMessage message, EntryInfo entryInfo, IDirectoryInfo destination, CancellationToken token)
    {
        return entryInfo.Descriptor switch
        {
            PegDescriptor => await PackPeg(message, entryInfo, destination, token),
            Str2Descriptor or VppDescriptor => await PackVpp(message, entryInfo, destination, token),
            LocatextDescriptor l => await EncodeLocalization(message, destination, l, token),
            {IsContainer: false} => await IgnoreNotContainer(message),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<FileResult> EncodeLocalization(StartPackMessage message, IDirectoryInfo destination, LocatextDescriptor descriptor, CancellationToken token)
    {
        var fileToEncode = fileSystem.FileInfo.New(message.FileSystemInfo.FullName);
        var (encodeEntryInfo, encodeStream) = await ProcessLocalization(fileToEncode, descriptor);
        var name = descriptor.GetEncodeName(encodeEntryInfo);
        var file = fileManager.CreateFileRecursive(destination, name, message.Settings.Force);
        await using var f = file.OpenWrite();
        log.LogDebug("Writing [{file}]", file.FullName);
        await encodeStream.CopyToAsync(f, token);
        return new FileResult(file, null);
    }

    private async Task<FileResult> PackVpp(StartPackMessage message, EntryInfo containerInfo, IDirectoryInfo destination, CancellationToken token)
    {
        var readEntries = await ProcessVppEntries(message, token);
        var entries = readEntries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
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

    private async Task<FileResult> PackPeg(StartPackMessage message, EntryInfo containerInfo, IDirectoryInfo destination, CancellationToken token)
    {
        var entries = await ProcessPegEntries(message, containerInfo, token);
        entries = entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
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

    private async Task<List<LogicalFile>> ProcessVppEntries(StartPackMessage message, CancellationToken token)
    {
        var directory = fileSystem.DirectoryInfo.New(message.FileSystemInfo.FullName);
        var result = new List<LogicalFile>();
        var files = directory.EnumerateFiles().ToList();
        var progress = new ProgressLogger($"Reading directory {message.FileSystemInfo.FullName}", files.Count, log);
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();

            var descriptor = FormatDescriptors.MatchForEncoding(file.Name);
            var (entry, data) = descriptor switch
            {
                LocatextDescriptor l => await ProcessLocalization(file, l),
                TextureDescriptor => throw new InvalidOperationException("Textures must be packed into PEG containers"),
                XmlDescriptor => throw new InvalidOperationException("Do not pack formatted xml"),
                _ => ProcessRegularFile(file)
            };
            if (entry.Properties.Index == null)
            {
                throw new InvalidOperationException($"Missing index property! [{entry}]");
            }
            result.Add(new LogicalFile(data, entry.Name, entry.Properties.Index!, null, null));
            progress.Tick();
        }

        return result;
    }

    private async Task<ProcessResult> ProcessLocalization(IFileInfo file, LocatextDescriptor locatextDescriptor)
    {
        var entry = locatextDescriptor.ReadEntryForEncoding(file);
        await using var f = file.OpenRead();
        var locatext = locatextReader.ReadXml(f, entry.Name);
        var ms = new MemoryStream();
        new LocatextWriter().Write(locatext, ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new(entry, ms);
    }

    private ProcessResult ProcessRegularFile(IFileInfo file)
    {
        var entryInfo = FormatDescriptors.RegularFile.ReadEntryForEncoding(file);
        if (entryInfo.Name.EndsWith(".asm_pc", StringComparison.OrdinalIgnoreCase))
        {
            log.LogWarning("File [{name}] is an asm_pc asset assembler file. Any changes to sibling files require updating this asm_pc file, which is not supported yet. Be careful with your changes, game may crash!", file.Name);
        }

        return new(entryInfo, file.OpenRead());
    }

    private async Task<List<LogicalTexture>> ProcessPegEntries(StartPackMessage message, EntryInfo containerInfo, CancellationToken token)
    {
        var directory = fileSystem.DirectoryInfo.New(message.FileSystemInfo.FullName);
        var result = new List<LogicalTexture>();
        var files = directory.EnumerateFiles().ToList();
        var progress = new ProgressLogger($"Reading directory {message.FileSystemInfo.FullName}", files.Count, log);
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            if (FormatDescriptors.MatchForEncoding(file.Name) is not TextureDescriptor textureDescriptor)
            {
                log.LogWarning("File [{name}] is not a texture, skipped", file.Name);
                continue;
            }

            var entry = textureDescriptor.ReadEntryForEncoding(file);
            await using var f = file.OpenRead();
            var tmp = textureDescriptor.ToTexture(entry);
            var texture = await imageConverter.ImageToTexture(f, entry.Properties.ImgFmt!.Value, tmp, token);
            result.Add(tmp with{Data = texture});
            progress.Tick();
        }

        return result;
    }

    private Task<FileResult> IgnoreNotContainer(StartPackMessage message)
    {
        log.LogTrace("Skipped packing [{path}]: not a container format", message.FileSystemInfo.FullName);
        return Task.FromResult(new FileResult(null, null));
    }

    private record FileResult(IFileInfo? Primary, IFileInfo? Secondary);

    private record ProcessResult(EntryInfo EntryInfo, Stream Data);
}
