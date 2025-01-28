using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services.Handlers;

public class UnpackHandler(ILogger<UnpackHandler> log) : HandlerBase<UnpackMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(UnpackMessage message, CancellationToken token)
    {
        try
        {
            log.LogTrace("Unpacking {entry} ", message.EntryInfo);
            ArgumentOutOfRangeException.ThrowIfNotEqual(message.Primary.Position, 0);
            ArgumentOutOfRangeException.ThrowIfNotEqual(message.Secondary?.Position ?? 0, 0);
            return await HandleInternal(message, token);
        }
        finally
        {
            await FormatUtils.DisposeNotNull(message.Primary);
            await FormatUtils.DisposeNotNull(message.Secondary);
        }
    }

    private async Task<IEnumerable<IMessage>> HandleInternal(UnpackMessage message, CancellationToken token)
    {
        if (!message.EntryInfo.Descriptor.IsContainer)
        {
            log.LogWarning("Not a container: [{name}]", message.EntryInfo.Name);
            return [];
        }

        if (message.EntryInfo.Descriptor.IsPaired && message.Secondary is null)
        {
            log.LogTrace("Skip [{name}] without secondary stream", message.EntryInfo.Name);
            return [];
        }

        return message.EntryInfo.Descriptor switch
        {
            VppDescriptor or Str2Descriptor => await UnpackVpp(message, token),
            PegDescriptor => await UnpackPeg(message, token),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Unpack vpp archive
    /// </summary>
    private async Task<IEnumerable<IMessage>> UnpackVpp(UnpackMessage message, CancellationToken token)
    {
        if (message.Secondary != null)
        {
            throw new InvalidOperationException("Unexpected secondary stream");
        }

        var result = new List<IMessage>();
        var reader = new VppReader(message.Settings.OptimizeFor);
        var logicalArchive = await Task.Run(() => reader.Read(message.Primary, message.EntryInfo.Name, token), token);
        var logicalFiles = logicalArchive.LogicalFiles.ToList();
        message.EntryInfo.Properties.VppMode = logicalArchive.Mode;
        message.EntryInfo.Properties.Entries = logicalFiles.Count;
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        var name = message.EntryInfo.Descriptor.GetDecodeName(message.EntryInfo, message.Settings.WriteProperties);
        var destination = message.Destination.Descend(name);
        var progress = new ProgressLogger($"Reading {relativePath}", logicalFiles.Count, log);
        // TODO filter by settings: type and glob
        foreach (var logicalFile in logicalFiles)
        {
            token.ThrowIfCancellationRequested();
            var secondary = LocateSecondaryIfPrimary(logicalFile, logicalFiles);
            var newMessages = ProcessVppEntry(logicalFile, secondary, breadcrumbs, destination, message.Settings);
            result.AddRange(newMessages);
            progress.Tick();
        }
        return result;
    }

    /// <summary>
    /// Unpack peg container
    /// </summary>
    private async Task<IEnumerable<IMessage>> UnpackPeg(UnpackMessage message, CancellationToken token)
    {
        if (message.Secondary == null)
        {
            throw new InvalidOperationException("Missing secondary stream");
        }

        var reader = new PegReader();
        var streams = new PairedStreams(message.Primary, message.Secondary);
        var logicalArchive = await Task.Run(() =>reader.Read(streams.Cpu, streams.Gpu, message.EntryInfo.Name, token), token);
        var logicalFiles = logicalArchive.LogicalTextures.ToList();
        message.EntryInfo.Properties.PegAlign = logicalArchive.Align;
        message.EntryInfo.Properties.Entries = logicalFiles.Count;
        var breadcrumbs = message.Breadcrumbs.Descend(logicalArchive.Name);
        var relativePath = breadcrumbs.ToString();
        var name = message.EntryInfo.Descriptor.GetDecodeName(message.EntryInfo, message.Settings.WriteProperties);
        var destination = message.Destination.Descend(name);
        var result = new List<IMessage>();
        var progress = new ProgressLogger($"Reading {relativePath}", logicalFiles.Count, log);
        foreach (var logicalFile in logicalFiles)
        {
            token.ThrowIfCancellationRequested();
            // peg entries can't contain nested containers or pairs
            var newMessages = ProcessPegEntry(logicalFile, breadcrumbs, destination, message.Settings);
            result.AddRange(newMessages);
            progress.Tick();
        }
        return result;
    }

    /// <summary>
    /// Find descriptor for vpp entry
    /// </summary>
    private IEnumerable<IMessage> ProcessVppEntry(LogicalFile logicalFile, Stream? secondary, Breadcrumbs breadcrumbs, IDirectoryInfo destination, UnpackSettings settings)
    {
        var properties = new Properties();
        properties.Index = logicalFile.Order;
        var entryDescriptor = FormatDescriptors.DetermineByName(logicalFile.Name);
        var entryInfo = new EntryInfo(logicalFile.Name, entryDescriptor, properties);
        return ProcessAnyNestedEntry(entryInfo, logicalFile.Content, secondary, breadcrumbs, destination, settings);
    }

    /// <summary>
    /// Collect texture properties
    /// </summary>
    private IEnumerable<IMessage> ProcessPegEntry(LogicalTexture logicalFile, Breadcrumbs breadcrumbs, IDirectoryInfo destination, UnpackSettings settings)
    {
        var properties = new Properties();
        properties.Index = logicalFile.Order;
        properties.TexFmt = logicalFile.Format;
        properties.TexFlags= logicalFile.Flags;
        properties.TexMips= logicalFile.MipLevels;
        properties.TexSize= logicalFile.Size;
        properties.TexSrc= logicalFile.Source;
        properties.PegAlign= logicalFile.Align;
        properties.ImgFmt= ImageFormat.raw;


        var entryDescriptor = FormatDescriptors.DetermineByName(logicalFile.Name);
        var entryInfo = new EntryInfo(logicalFile.Name, entryDescriptor, properties);
        return ProcessAnyNestedEntry(entryInfo, logicalFile.Data, null, breadcrumbs, destination, settings);
    }

    /// <summary>
    /// Generate new tasks depending on settings
    /// </summary>
    private IEnumerable<IMessage> ProcessAnyNestedEntry(EntryInfo entryInfo, Stream primary, Stream? secondary, Breadcrumbs breadcrumbs, IDirectoryInfo destination, UnpackSettings settings)
    {
        if (entryInfo.Descriptor.IsContainer == false || settings.SkipContainers == false)
        {
            yield return new WriteFileMessage(entryInfo, primary.MakeDeepOwnCopy(), breadcrumbs, destination, settings);
        }

        if (entryInfo.Descriptor.IsContainer && settings.Recursive)
        {
            var nestedDestination = FormatUtils.Descend(destination, Constants.DefaultUnpackDir);
            yield return new UnpackMessage(entryInfo, primary.MakeDeepOwnCopy(), secondary?.MakeDeepOwnCopy(), breadcrumbs, nestedDestination, settings);
        }

        if (settings.Metadata)
        {
            yield return new BuildMetadataMessage(entryInfo, breadcrumbs, primary.MakeDeepOwnCopy());
        }
    }

    /// <summary>
    /// If paired format, for cpu file, return gpu file
    /// </summary>
    private Stream? LocateSecondaryIfPrimary(LogicalFile logicalFile, IReadOnlyList<LogicalFile> entries)
    {
        var cpu = PairedFiles.GetCpuFileName(logicalFile.Name);
        var gpu = PairedFiles.GetGpuFileName(logicalFile.Name);
        var gpuEntry = entries.Skip(logicalFile.Order).FirstOrDefault(x => x.Name == gpu);
        if (logicalFile.Name == cpu && gpuEntry != null)
        {
            return gpuEntry.Content;
        }

        return null;
    }
}
