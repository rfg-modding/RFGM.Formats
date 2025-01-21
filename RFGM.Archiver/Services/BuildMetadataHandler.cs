using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Messages;
using RFGM.Archiver.Models.Metadata;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class BuildMetadataHandler(ILogger<BuildMetadataHandler> log) : HandlerBase<BuildMetadataMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(BuildMetadataMessage message, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var name = message.Name;
        var path = message.Breadcrumbs.Descend(name).ToString();
        log.LogInformation("Building metadata for {item}", path);
        var hash = await message.GetHash(token);
        var length = message.Length;
        IMetadata metadata = message.Item switch
        {
            LogicalArchive vpp => new VppArchive(name, path, vpp.Mode, length, hash, message.NestedCount),
            LogicalFile file => new VppEntry(name, path, file.Order, file.Offset, length, file.CompressedSize, hash),
            LogicalTextureArchive peg => new PegArchive(name, path, length, peg.Align, hash, message.NestedCount),
            LogicalTexture tex => new PegEntry(name, path, tex.Order, (uint) tex.DataOffset, length, tex.Size, tex.Source, tex.AnimTiles, tex.Format, tex.Flags, tex.MipLevels, tex.Align, hash),
            _ => throw new ArgumentOutOfRangeException()
        };
        Archiver.Metadata.Add(metadata);
        return [];
    }
}
