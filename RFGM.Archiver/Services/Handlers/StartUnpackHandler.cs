using System.IO.Abstractions;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Streams;

namespace RFGM.Archiver.Services.Handlers;

public class StartUnpackHandler(IFileSystem fileSystem, ArchiverState archiverState) : HandlerBase<StartUnpackMessage>
{
    public override Task<IEnumerable<IMessage>> Handle(StartUnpackMessage message, CancellationToken token)
    {
        var destination = fileSystem.Path.IsPathFullyQualified(message.Destination)
            ? fileSystem.DirectoryInfo.New(message.Destination)
            : fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(message.FileInfo.Directory!.FullName, message.Destination));

        var descriptor = FormatDescriptors.MatchForDecoding(message.FileInfo.Name);
        var (nameWithoutProps, _, properties) = FormatDescriptors.RegularFile.ReadEntryForEncoding(message.FileInfo);
        var entryInfo = new EntryInfo(nameWithoutProps, descriptor, properties);
        var secondaryFile = LocateSecondaryIfPrimary(message.FileInfo);
        archiverState.RememberDestination(destination.FullName);
        var result = new List<IMessage>();
        result.Add(new UnpackMessage(entryInfo, message.FileInfo.OpenRead(), secondaryFile?.OpenRead(), Breadcrumbs.Init(), destination, message.Settings));
        if (message.Settings.Metadata)
        {
            result.Add(new CollectMetadataMessage(entryInfo, Breadcrumbs.Init(), message.FileInfo.OpenRead()));
            if (secondaryFile is not null)
            {
                var secondaryEntryInfo = new EntryInfo(secondaryFile.Name, descriptor, new Properties());
                result.Add(new CollectMetadataMessage(secondaryEntryInfo, Breadcrumbs.Init(), secondaryFile.OpenRead()));
            }
        }
        return Task.FromResult<IEnumerable<IMessage>>(result);
    }

    /// <summary>
    /// If paired format, for cpu file, return gpu file
    /// </summary>
    private IFileInfo? LocateSecondaryIfPrimary(IFileInfo file)
    {
        var nameWithoutProps = FormatDescriptors.RegularFile.ReadEntryForEncoding(file).Name;
        var byName = file.Directory!.EnumerateFiles()
            .Select(x => new {FormatDescriptors.RegularFile.ReadEntryForEncoding(x).Name, x});
        var cpu = PairedFiles.GetCpuFileName(nameWithoutProps);
        var gpu = PairedFiles.GetGpuFileName(nameWithoutProps);
        var gpuFile = byName.FirstOrDefault(x => x.Name == gpu)?.x;
        if (nameWithoutProps == cpu && gpuFile != null)
        {
            return gpuFile;
        }

        return null;
    }
}
