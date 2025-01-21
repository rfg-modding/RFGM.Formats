using RFGM.Archiver.Models.Metadata;
using RFGM.Formats;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Models.Messages;

public record BuildMetadataMessage(object Item, Breadcrumbs Breadcrumbs, object Data, int NestedCount, bool Hash) : IMessage
{
    public async Task<string> GetHash(CancellationToken token)
    {
        if (!Hash)
        {
            return string.Empty;
        }

        return Data switch
        {
            Stream s => await Utils.ComputeHash(s, token),
            PairedStreams ps => await Utils.ComputeHash(ps, token),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string Length { get; } = Data switch
    {
        Stream s => s.Length.ToString(),
        PairedStreams ps => ps.Size,
        _ => throw new ArgumentOutOfRangeException()
    };



    public string Name { get; } = Item switch
    {
        LogicalArchive vpp => vpp.Name,
        LogicalFile file => file.Name,
        LogicalTextureArchive peg => peg.Name,
        LogicalTexture tex => tex.Name,
        _ => throw new ArgumentOutOfRangeException()
    };
}
