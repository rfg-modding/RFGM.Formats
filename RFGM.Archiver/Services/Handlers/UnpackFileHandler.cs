using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Streams;

namespace RFGM.Archiver.Services.Handlers;

public class UnpackFileHandler(IFileSystem fileSystem, ILogger<UnpackFileHandler> log) : HandlerBase<UnpackFileMessage>
{
    public override Task<IEnumerable<IMessage>> Handle(UnpackFileMessage message, CancellationToken token)
    {
        var destination = fileSystem.Path.IsPathFullyQualified(message.Destination)
            ? fileSystem.DirectoryInfo.New(message.Destination)
            : fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(message.FileInfo.Directory!.FullName, message.Destination));

        var descriptor = FormatDescriptors.DetermineByName(message.FileInfo.Name);
        if (!descriptor.IsContainer)
        {
            log.LogWarning("Not a container: [{name}]", message.FileInfo.Name);
            return Task.FromResult(Enumerable.Empty<IMessage>());
        }
        var entryInfo = new EntryInfo(message.FileInfo.Name, descriptor, new Properties());
        var secondaryFile = LocateSecondaryIfPrimary(message.FileInfo);
        Archiver.Destinations.Add(destination.FullName);
        var result = new List<IMessage>();
        result.Add(new UnpackMessage(entryInfo, message.FileInfo.OpenRead(), secondaryFile?.OpenRead(), Breadcrumbs.Init(), destination, message.Settings));
        if (message.Settings.Metadata)
        {
            result.Add(new BuildMetadataMessage(entryInfo, Breadcrumbs.Init(), message.FileInfo.OpenRead()));
            if (secondaryFile is not null)
            {
                var secondaryEntryInfo = new EntryInfo(secondaryFile.Name, descriptor, new Properties());
                result.Add(new BuildMetadataMessage(secondaryEntryInfo, Breadcrumbs.Init(), secondaryFile.OpenRead()));
            }
        }
        return Task.FromResult<IEnumerable<IMessage>>(result);
    }

    /// <summary>
    /// If paired format, for cpu file, return gpu file
    /// </summary>
    private IFileInfo? LocateSecondaryIfPrimary(IFileInfo file)
    {
        var nameWithoutNumber = FormatDescriptors.RegularFile.FromFileSystem(file).Name;
        var byName = file.Directory!.EnumerateFiles()
            .Select(x => new {FormatDescriptors.RegularFile.FromFileSystem(x).Name, x});
        var cpu = PairedFiles.GetCpuFileName(nameWithoutNumber);
        var gpu = PairedFiles.GetGpuFileName(nameWithoutNumber);
        var gpuFile = byName.FirstOrDefault(x => x.Name == gpu)?.x;
        if (nameWithoutNumber == cpu && gpuFile != null)
        {
            return gpuFile;
        }

        return null;
    }
}
