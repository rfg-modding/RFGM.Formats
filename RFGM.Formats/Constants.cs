using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RFGM.Formats;

public static class Constants
{
    public const string DefaultDir = ".unpack";
    public const string DefaultOutputDir = ".pack";
    public const string MetadataFile = ".metadata";

    public static readonly Regex VppEntryNameFormat = new (@"^(?<order>\d+?)\s+(?<name>.*?)$", RegexOptions.Compiled);

    /// <summary>
    /// Name format for new textures: "name format mipLevels.png". Example: "my_texture rgba_srgb 5.png"
    /// </summary>
    public static readonly Regex TextureNameFormat = new (@"^(?<name>.*?)\s+(?<format>.*?)\s+(?<mipLevels>\d+).png$", RegexOptions.Compiled);


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
