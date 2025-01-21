using RFGM.Formats.Peg.Models;

namespace RFGM.Archiver.Models.Metadata;

public record PegEntry(
    string Name,
    string RelativePath,
    int Order,
    uint Offset,
    string Length,
    Size Size,
    Size Source,
    Size AnimTiles,
    RfgCpeg.Entry.BitmapFormat Format,
    TextureFlags Flags,
    int MipLevels,
    int Align,
    string Hash) : IMetadata
{
    public override string ToString() => $"{RelativePath} {nameof(Order)}={Order} {nameof(Offset)}={Offset} {nameof(Length)}={Length} " +
                                         $"{nameof(Size)}={Size} {nameof(Source)}={Source} {nameof(AnimTiles)}={AnimTiles} " +
                                         $"{nameof(Format)}={Format} {nameof(Flags)}={Flags} {nameof(MipLevels)}={MipLevels} {nameof(Align)}={Align} " +
                                         $"{nameof(Hash)}={Hash}";
}
