using System.IO.Abstractions;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;

namespace RFGM.Formats.Abstractions.Descriptors;

/// <summary>
/// tga files (DDS textures without header), converted textures (png, dds, raw)
/// </summary>
public class TextureDescriptor : FormatDescriptorBase
{
    public override bool IsPaired => false;
    public override bool IsContainer => false;
    protected override List<string> CanDecodeExt => [Extension];

    protected override List<string> CanEncodeExt =>
    [
        ".png",
        ".dds",
        ".raw",
    ];

    /// <summary>
    /// Decoded texture extension depends on ImgFmt property
    /// </summary>
    protected override string GetDecodeNameInternal(EntryInfo data, bool encodeProperties)
    {
        var (name, originalExt) = FormatUtils.GetNameAndFullExt(data.Name);
        if (!Extension.Equals(originalExt, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{Name}: Invalid extension [{originalExt}]");
        }

        var newExt = '.' + data.Properties.ImgFmt.ToString()!.ToLowerInvariant();
        var props = data.Properties.Serialize();
        return $"{name} {props}{newExt}";
    }

    /// <summary>
    /// Read ImgFmt from extension
    /// </summary>
    protected override EntryInfo FromFileSystemInternal(IFileSystemInfo fileSystemInfo)
    {
        var result = base.FromFileSystemInternal(fileSystemInfo);
        var (_, ext) = FormatUtils.GetNameAndFullExt(fileSystemInfo.Name);
        var imgFmt = Enum.Parse<ImageFormat>(ext[1..]);
        result.Properties.ImgFmt = imgFmt;
        return result;
    }

    protected override string GetEncodeExt(string originalExt)
    {
        if (!CanEncodeExtensions.Contains(originalExt))
        {
            throw new ArgumentException($"{Name}: Invalid extension [{originalExt}]");
        }

        return Extension;
    }

    public LogicalTexture ToTexture(EntryInfo info)
    {
        var p = info.Properties;
        ArgumentNullException.ThrowIfNull(p.TexSize);
        ArgumentNullException.ThrowIfNull(p.TexSrc);
        ArgumentNullException.ThrowIfNull(p.TexFmt);
        ArgumentNullException.ThrowIfNull(p.TexFlags);
        ArgumentNullException.ThrowIfNull(p.TexMips);
        ArgumentNullException.ThrowIfNull(p.PegAlign);
        return new LogicalTexture(p.TexSize, p.TexSrc, Size.Zero, p.TexFmt.Value, p.TexFlags.Value, p.TexMips, p.Index ?? -1, info.Name, 0, 0, p.PegAlign, Stream.Null);
    }

    public static readonly string Extension = ".tga";

}
