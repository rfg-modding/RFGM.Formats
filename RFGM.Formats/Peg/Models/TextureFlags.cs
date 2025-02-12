namespace RFGM.Formats.Peg.Models;

/// <summary>
/// Flags used in peg archive
/// </summary>
/// <remarks>not read with kaitai directly for simplicity</remarks>
[Flags]
public enum TextureFlags
{
    None = 0,
    Unknown0 = 1 << 0,
    Unknown1 = 1 << 1,
    Unknown2 = 1 << 2,
    CubeTexture = 1 << 3,
    Unknown4 = 1 << 4,
    Unknown5 = 1 << 5,
    Unknown6 = 1 << 6,
    Unknown7 = 1 << 7,
    HasAnimTiles = 1 << 8,
    Srgb = 1 << 9,
    Unknown10 = 1 << 10,
    Unknown11 = 1 << 11,
    Unknown12 = 1 << 12,
    Unknown13 = 1 << 13,
    Unknown14 = 1 << 14,
    Unknown15 = 1 << 15,

}
