using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services.Handlers;

public class PackDirectoryHandler(IFileSystem fileSystem, FileManager fileManager, ImageConverter imageConverter, ILogger<PackDirectoryHandler> log) : HandlerBase<PackDirectoryMessage>
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
            RegularFileDescriptor or RawTextureDescriptor => await IgnoreNotContainer(message),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<FileResult> PackVpp(PackDirectoryMessage message, EntryInfo containerInfo, IDirectoryInfo destination, CancellationToken token)
    {
        var entries = ProcessVppEntries(message, token)
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

        var container = new LogicalArchive(entries, containerInfo.Properties.Get<RfgVpp.HeaderBlock.Mode>(Properties.Mode), containerInfo.Name);
        using var writer = new VppWriter(container);
        var file = fileManager.CreateFileRecursive(destination, containerInfo.FileName, message.Settings.Force);
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

        var container = new LogicalTextureArchive(entries, containerInfo.Name, 0, 0, containerInfo.Properties.Get<int>(Properties.Align));
        using var writer = new PegWriter(container);
        var cpuFile = fileManager.CreateFileRecursive(destination, PairedFiles.GetCpuFileName(containerInfo.FileName)!, message.Settings.Force);
        var gpuFile = fileManager.CreateFileRecursive(destination, PairedFiles.GetGpuFileName(containerInfo.FileName)!, message.Settings.Force);
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

    private IEnumerable<LogicalFile> ProcessVppEntries(PackDirectoryMessage message, CancellationToken token)
    {
        var files = message.DirectoryInfo.EnumerateFiles().ToList();
        var progress = new ProgressLogger($"Reading directory {message.DirectoryInfo.FullName}", files.Count, log);
        foreach (var file in message.DirectoryInfo.EnumerateFiles())
        {
            token.ThrowIfCancellationRequested();
            // pack all files, no special cases yet. TODO: update asm_pc, detect when it's needed
            var entryInfo = FormatDescriptors.RegularFile.FromFileSystem(file);
            if (entryInfo.Name.EndsWith(".asm_pc", StringComparison.OrdinalIgnoreCase))
            {
                log.LogWarning("File [{name}] is an asm_pc asset assembler file. Any changes to sibling files require updating this asm_pc file, which is not supported yet. Be careful with your changes, game may crash!", file.Name);
            }
            yield return new LogicalFile(file.OpenRead(), entryInfo.Name, entryInfo.Properties.GetOrDefault(Properties.Index, -1), null, null);
            progress.Tick();
        }
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

            var (name, _, props) = textureDescriptor.FromFileSystem(file);
            var order = props.GetOrDefault(Properties.Index, -1);
            var format = props.Get<RfgCpeg.Entry.BitmapFormat>(Properties.Format);
            var flags = props.Get<TextureFlags>(Properties.Flags);
            var mipLevels = props.Get<int>(Properties.MipLevels);
            var size = props.Get<Size>(Properties.Size);
            var source = props.Get<Size>(Properties.Source);
            var align = containerInfo.Properties.Get<int>(Properties.Align);
            var imageFormat = props.Get<ImageFormat>(Properties.ImageFormat);
            await using var f = file.OpenRead();
            var tmp = new LogicalTexture(size, source, Size.Zero, format, flags, mipLevels, order, name, 0, 0, align, Stream.Null);
            var texture = await imageConverter.ImageToTexture(f, imageFormat, tmp, token);
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
}
