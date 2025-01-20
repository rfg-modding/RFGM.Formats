using System.Collections.Immutable;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Formats;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Models;

public class FormatManager(IFileSystem fileSystem, ILogger<FormatManager> log)
{
    public static readonly ImmutableHashSet<FileFormat> CanRead = new HashSet<FileFormat>
    {
        FileFormat.Vpp,
        FileFormat.Str2,
        FileFormat.Peg
    }.ToImmutableHashSet();

    public static readonly ImmutableHashSet<FileFormat> CanWriteFull = new HashSet<FileFormat>
    {
        FileFormat.Vpp,
        FileFormat.Peg
    }.ToImmutableHashSet();

    public static readonly ImmutableHashSet<FileFormat> CanWritePartial = new HashSet<FileFormat>
    {
        FileFormat.Str2,
    }.ToImmutableHashSet();

    public FileFormat GuessFormatByExtension(IFileSystemInfo item) => GuessFormatByExtension(item.Name);

    public FileFormat GuessFormatByExtension(string name)
    {
        var firstDot = name.IndexOf('.', StringComparison.Ordinal);
        if (firstDot == -1)
        {
            return FileFormat.Unsupported;
        }
        var extension = name[firstDot..].ToLowerInvariant();
        log.LogDebug("{path} => {ext}", name, extension);
        // previously unpacked vpp directories have ".vpp_pc.mode" extension
        if (extension.Contains(".vpp_pc"))
        {
            return FileFormat.Vpp;
        }
        // previously unpacked peg directories have ".cpeg_pc.align" extension
        if (extension.Contains(".cpeg_pc") || extension.Contains(".cvbm_pc") || extension.Contains(".gpeg_pc") || extension.Contains(".gvbm_pc"))
        {
            return FileFormat.Peg;
        }

        return extension switch
        {
            ".vpp_pc" => FileFormat.Vpp,
            ".str2_pc" => FileFormat.Str2,
            ".cpeg_pc" or ".gpeg_pc" or ".cvbm_pc" or ".gvbm_pc" => FileFormat.Peg,
            _ => FileFormat.Unsupported
        };
    }

    public (RfgVpp.HeaderBlock.Mode mode, string name) ParseVppInfo(IDirectoryInfo directoryInfo)
    {
        var match = Constants.VppDirectoryNameFormat.Match(directoryInfo.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Directory name is not in valid format: [{directoryInfo.FullName}]");
        }

        var mode = Enum.Parse<RfgVpp.HeaderBlock.Mode>(match.Groups["mode"].Value, true);
        var name = match.Groups["name"].Value;
        return (mode, name);
    }

    public (int align, string name) ParsePegInfo(IDirectoryInfo directoryInfo)
    {
        var match = Constants.PegDirectoryNameFormat.Match(directoryInfo.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Directory name is not in valid format: [{directoryInfo.FullName}]");
        }

        var align = int.Parse(match.Groups["align"].Value);
        var name = match.Groups["name"].Value;
        return (align, name);
    }

    public (int order, string name) ParseVppEntryInfo(IFileInfo fileInfo)
    {
        var match = Constants.VppEntryNameFormat.Match(fileInfo.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Filename is not in valid format: [{fileInfo.FullName}]");
        }

        var order = int.Parse(match.Groups["order"].Value);
        var name = match.Groups["name"].Value;
        return (order, name);
    }

    public (int order, string name, ImageFormat imageFormat, RfgCpeg.Entry.BitmapFormat format, TextureFlags flags, int mipLevels, Size size, Size source) ParsePegEntryInfo(IFileInfo fileInfo)
    {
        var match = Constants.TextureNameFormat.Match(fileInfo.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Filename is not in valid format: [{fileInfo.FullName}]");
        }
        var order = int.Parse(match.Groups["order"].Value);
        var name = match.Groups["name"].Value;
        var format = Enum.Parse<RfgCpeg.Entry.BitmapFormat>(match.Groups["format"].Value, true);
        var flags = Enum.Parse<TextureFlags>(match.Groups["flags"].Value, true);
        var mipLevels = int.Parse(match.Groups["mipLevels"].Value);
        var imageFormat = Enum.Parse<ImageFormat>(match.Groups["ext"].Value, true);
        var size = Size.Parse(match.Groups["size"].Value);
        var source = Size.Parse(match.Groups["source"].Value);

        return (order, name, imageFormat, format, flags, mipLevels, size, source);
    }
}
