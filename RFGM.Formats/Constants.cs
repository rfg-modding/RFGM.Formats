using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RFGM.Formats;

public static class Constants
{
    public const string DefaultDir = ".unpack";
    public const string DefaultOutputDir = ".pack";
    public const string MetadataFile = ".metadata";

    /// <summary>
    /// Name format for unpacked vpp folders: name.vpp_pc.mode
    /// </summary>
    /// <example>misc.vpp_pc.compressed</example>
    /// <example>items.vpp_pc.normal</example>
    public static readonly Regex VppDirectoryNameFormat = new (@"^(?<name>(.*?).vpp_pc)\.(?<mode>.*?)$", RegexOptions.Compiled);

    /// <summary>
    /// Name format for unpacked peg folders: name.cpeg_pc.align
    /// </summary>
    /// <example>new_jetpack.cpeg_pc.16</example>
    /// <example>items_containers.cpeg_pc.256</example>
    public static readonly Regex PegDirectoryNameFormat = new (@"^(?<name>.*?)\.(?<align>\d+?)$", RegexOptions.Compiled);

    /// <summary>
    /// Name format for files unpacked from vpp/str2: "00001 name"
    /// </summary>
    /// /// <example>00001 weapons.xtbl</example>
    /// /// <example>00002 something.str2_pc</example>
    public static readonly Regex VppEntryNameFormat = new (@"^(?<order>\d+?)\s+(?<name>.*?)$", RegexOptions.Compiled);

    /// <summary>
    /// Name format for textures unpacked from peg: "00001 name.tga format mipLevels.png"
    /// </summary>
    /// <example>00001 my_texture.tga rgba_srgb 5 100x100.png</example>
    /// <example>00025 texture.tga dxt1 0 256x1024.dds</example>
    public static readonly Regex TextureNameFormat = new (@"^(?<order>\d+?)\s+(?<name>.*?)\s+(?<format>.*?)\s+(?<flags>.*?)\s+(?<mipLevels>\d+)\s+(?<size>\d+x\d+)\s+(?<source>\d+x\d+).(?<ext>.*?)$", RegexOptions.Compiled);


    public static readonly ImmutableHashSet<string> KnownVppExtensions = new HashSet<string>
    {
        "vpp",
        "vpp_pc",
        "str2",
        "str2_pc"
    }.ToImmutableHashSet();

    public static readonly ImmutableHashSet<string> KnownPegExtensions = new HashSet<string>
    {
        "cpeg_pc",
        "cvbm_pc",
        "gpeg_pc",
        "gvbm_pc",
    }.ToImmutableHashSet();

}
