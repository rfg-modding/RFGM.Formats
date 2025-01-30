using System.Xml;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Abstractions.Descriptors;
using RFGM.Formats.Localization;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Services.Handlers;

public class DecodeFileHandler(FileManager fileManager, ImageConverter imageConverter, LocatextReader locatextReader, ILogger<DecodeFileHandler> log) : HandlerBase<DecodeFileMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(DecodeFileMessage message, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(message.Primary.Position, 0);
        await using (message.Primary)
        {
            await HandleInternal(message, token);
        }

        return [];
    }

    private async Task HandleInternal(DecodeFileMessage message, CancellationToken token)
    {
        var path = message.Breadcrumbs.Descend(message.EntryInfo.Name).ToString();
        if (!message.Settings.Matcher.Match(path).HasMatches)
        {
            log.LogTrace("Skipped writing [{path}]: did not match specified pattern", path);
            return;
        }

        var task = message.EntryInfo.Descriptor switch
        {
            {IsContainer: true} when message.Settings.SkipContainers => IgnoreContainer(path),
            XmlDescriptor when message.Settings.XmlFormat => WriteXml(message, token),
            TextureDescriptor t => WriteTexture(t, message, token),
            LocatextDescriptor when message.Settings.Localization => WriteLocalization(message),
            _ => WriteFile(message, token)
        };

        await task;
    }

    private async Task WriteLocalization(DecodeFileMessage message)
    {
        var locatext = locatextReader.Read(message.Primary, message.EntryInfo.Name, []);
        var name = message.EntryInfo.Descriptor.GetDecodeName(message.EntryInfo, message.Settings.WriteProperties);
        var file = fileManager.CreateFileRecursive(message.Destination, name, message.Settings.Force);
        await using var f = file.OpenWrite();
        log.LogDebug("Writing [{file}]", file.FullName);
        new LocatextWriter().WriteXml(locatext, f);
    }

    private async Task WriteFile(DecodeFileMessage message, CancellationToken token)
    {
        var fileDescriptor = FormatDescriptors.RegularFile;
        var name = fileDescriptor.GetDecodeName(message.EntryInfo, message.Settings.WriteProperties);
        var file = fileManager.CreateFileRecursive(message.Destination, name, message.Settings.Force);
        await using var f = file.OpenWrite();
        log.LogDebug("Writing [{file}]", file.FullName);
        await message.Primary.CopyToAsync(f, token);
    }

    private async Task WriteXml(DecodeFileMessage message, CancellationToken token)
    {
        var xml = new XmlDocument();
        using var reader = new StreamReader(message.Primary);
        xml.Load(reader);
        using var ms = new MemoryStream();
        FormatUtils.SerializeToMemoryStream(xml, ms, true);
        var name = message.EntryInfo.Descriptor.GetDecodeName(message.EntryInfo, message.Settings.WriteProperties);
        var file = fileManager.CreateFileRecursive(message.Destination, name, message.Settings.Force);
        await using var f = file.OpenWrite();
        log.LogDebug("Writing [{file}]", file.FullName);
        await ms.CopyToAsync(f, token);
    }

    private async Task WriteTexture(TextureDescriptor descriptor, DecodeFileMessage message, CancellationToken token)
    {
        var entry = message.EntryInfo;
        var texture = descriptor.ToTexture(entry) with {Data = message.Primary};
        var imageFormat = message.Settings.ImageFormat;
        var image = await imageConverter.TextureToImage(texture, imageFormat, token);
        var newEntry = entry with {Properties = entry.Properties with {ImgFmt = message.Settings.ImageFormat}};
        var name = newEntry.Descriptor.GetDecodeName(newEntry, message.Settings.WriteProperties);
        var file = fileManager.CreateFileRecursive(message.Destination, name, message.Settings.Force);
        await using var f = file.OpenWrite();
        log.LogDebug("Writing [{file}]", file.FullName);
        await image.CopyToAsync(f, token);
    }

    private Task IgnoreContainer(string path)
    {
        log.LogTrace("Skipped writing [{path}]: skip containers in settings", path);
        return Task.CompletedTask;
    }
}
