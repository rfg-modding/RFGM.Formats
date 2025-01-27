using System.Xml;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;

namespace RFGM.Archiver.Services.Handlers;

public class WriteFileHandler(FileManager fileManager, ImageConverter imageConverter, ILogger<WriteFileHandler> log) : HandlerBase<WriteFileMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(WriteFileMessage message, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(message.Primary.Position, 0);
        await using (message.Primary)
        {
            await HandleInternal(message, token);
        }

        return [];
    }

    private async Task HandleInternal(WriteFileMessage message, CancellationToken token)
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
            RawTextureDescriptor t => WriteTexture(t, message, token),
            _ => WriteFile(message, token)
        };

        await task;
    }

    private async Task WriteFile(WriteFileMessage message, CancellationToken token)
    {
        var file = fileManager.CreateFileRecursive(message.Destination, message.EntryInfo.FileName, message.Settings.Force);
        await using var f = file.OpenWrite();
        await message.Primary.CopyToAsync(f, token);
    }

    private async Task WriteXml(WriteFileMessage message, CancellationToken token)
    {
        var xml = new XmlDocument();
        using var reader = new StreamReader(message.Primary);
        xml.Load(reader);
        using var ms = new MemoryStream();
        FormatUtils.SerializeToMemoryStream(xml, ms, true);
        var file = fileManager.CreateFileRecursive(message.Destination, message.EntryInfo.FileName, message.Settings.Force);
        await using var f = file.OpenWrite();
        await ms.CopyToAsync(f, token);
    }

    private async Task WriteTexture(RawTextureDescriptor descriptor, WriteFileMessage message, CancellationToken token)
    {
        var props = message.EntryInfo.Properties;
        var size = props.Get<Size>(Properties.Size);
        var source = props.Get<Size>(Properties.Source);
        var bitmapFormat = props.Get<RfgCpeg.Entry.BitmapFormat>(Properties.Format);
        var flags = props.Get<TextureFlags>(Properties.Flags);
        var mipLevels = props.Get<int>(Properties.MipLevels);
        var align = props.Get<int>(Properties.Align);
        var texture = new LogicalTexture(size, source, Size.Zero, bitmapFormat, flags, mipLevels, 0, message.EntryInfo.Name, 0, 0, align, message.Primary);
        var imageFormat = message.Settings.ImageFormat;
        var image = await imageConverter.TextureToImage(texture, imageFormat, token);
        var name = descriptor.GetFileSystemName(message.EntryInfo, imageFormat);
        var file = fileManager.CreateFileRecursive(message.Destination, name, message.Settings.Force);
        await using var f = file.OpenWrite();
        await image.CopyToAsync(f, token);
    }

    private Task IgnoreContainer(string path)
    {
        log.LogTrace("Skipped writing [{path}]: skip containers in settings", path);
        return Task.CompletedTask;
    }
}
