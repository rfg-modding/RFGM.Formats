namespace RFGM.Formats.Abstractions;

public enum Format
{
    /// <summary>
    /// vpp_pc archives
    /// </summary>
    Vpp,

    /// <summary>
    /// str2_pc files (like vpp + asm metadata)
    /// </summary>
    Str2,

    /// <summary>
    /// cpeg_pc+gpeg_pc and cvbm_pc+gvbm_pc
    /// </summary>
    Peg,

    /// <summary>
    /// tga files as DDS textures without header
    /// </summary>
    RawTexture,

    /// <summary>
    /// converted textures: png, dds, raw
    /// </summary>
    Texture,

    /// <summary>
    /// xml-like files: xtbl, dtdox, gtodx
    /// </summary>
    Xml,

    /// <summary>
    /// rfglocatext
    /// </summary>
    Localization,

    /// <summary>
    /// just a file
    /// </summary>
    RegularFile
}
