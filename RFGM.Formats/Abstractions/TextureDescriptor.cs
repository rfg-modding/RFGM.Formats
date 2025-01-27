using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;

namespace RFGM.Formats.Abstractions;

public class TextureDescriptor : IFormatDescriptor
{
    public Format Format => Format.Texture;
    public bool CanDecode => false;
    public bool CanEncode => true;
    public bool IsPaired => false;
    public bool IsContainer => false;

    public ImmutableHashSet<string> Extensions { get; } = new HashSet<string>
    {
        ".png",
        ".dds",
        ".raw",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    public bool Match(string name)
    {
        return false;
    }

    public bool Match(IFileSystemInfo fileSystemInfo)
    {
        // unpacked textures are like "name.tga properties.png"
        return Extensions.Contains(fileSystemInfo.Extension) &&
               fileSystemInfo.Name.Contains(".tga", StringComparison.OrdinalIgnoreCase);
    }

    public EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo)
    {
        if (fileSystemInfo is not IFileInfo f)
        {
            throw new ArgumentException("Expected file");
        }

        var match = TextureNameFormat.Match(f.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"File name is not in valid format: [{f.FullName}]");
        }

        var name = match.Groups["name"].Value;
        var orderStr = match.Groups["order"].Value;
        var format = Enum.Parse<RfgCpeg.Entry.BitmapFormat>(match.Groups["format"].Value, true);
        var flags = Enum.Parse<TextureFlags>(match.Groups["flags"].Value, true);
        var mipLevels = int.Parse(match.Groups["mipLevels"].Value);
        var imageFormat = Enum.Parse<ImageFormat>(match.Groups["ext"].Value, true);
        var size = Size.Parse(match.Groups["size"].Value);
        var source = Size.Parse(match.Groups["source"].Value);
        var properties = new Properties();
        properties.Add(Properties.Format, format);
        properties.Add(Properties.Flags, flags);
        properties.Add(Properties.MipLevels, mipLevels);
        properties.Add(Properties.ImageFormat, imageFormat);
        properties.Add(Properties.Size, size);
        properties.Add(Properties.Source, source);
        if (!string.IsNullOrWhiteSpace(orderStr))
        {
            properties.Add(Properties.Index, int.Parse(orderStr));
        }

        return new EntryInfo(name, this, properties);
    }

    public string GetFileName(EntryInfo data)
    {
        var imageFormat = data.Properties.Get<ImageFormat>(Properties.ImageFormat);
        return GetFileSystemName(data, imageFormat);
    }

    public string GetFileSystemName(EntryInfo data, ImageFormat imageFormat)
    {
        var order = data.Properties.GetOrDefault<int?>(Properties.Index, null);
        var orderStr = order is null ? string.Empty : $"{order:D5} ";
        var format = data.Properties.Get<RfgCpeg.Entry.BitmapFormat>(Properties.Format);
        var flags = data.Properties.Get<TextureFlags>(Properties.Flags);
        var mipLevels = data.Properties.Get<int>(Properties.MipLevels);
        var size = data.Properties.Get<Size>(Properties.Size);
        var source = data.Properties.Get<Size>(Properties.Source);
        return $"{orderStr}{data.Name} {format} {flags} {mipLevels} {size} {source}.{imageFormat}";
    }

    public string GetDirectoryName(EntryInfo data)
    {
        throw new InvalidOperationException("Not a container");
    }

    /// <summary>
    /// Name format for textures unpacked from peg: "00001 name.tga format mipLevels.png"
    /// </summary>
    /// <example>my_texture.tga rgba_srgb 5 100x100.png</example>
    /// <example>00025 texture.tga dxt1 0 256x1024.dds</example>
    public static readonly Regex TextureNameFormat = new (@"^((?<order>\d+)\s+)?(?<name>.*?)\s+(?<format>.*?)\s+(?<flags>.*?)\s+(?<mipLevels>\d+)\s+(?<size>\d+x\d+)\s+(?<source>\d+x\d+).(?<ext>.*?)$", RegexOptions.Compiled);
}
