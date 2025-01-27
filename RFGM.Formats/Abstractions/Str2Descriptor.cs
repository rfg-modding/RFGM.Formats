using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Abstractions;

public class Str2Descriptor : IFormatDescriptor
{
    public Format Format => Format.Str2;
    public bool CanDecode => true;
    public bool CanEncode => true;
    public bool IsPaired => false;
    public bool IsContainer => true;
    public ImmutableHashSet<string> Extensions { get; } = new HashSet<string>
    {
        ".str2_pc"

    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    public bool Match(string name)
    {
        return name.EndsWith(".str2_pc", StringComparison.OrdinalIgnoreCase);
    }

    public bool Match(IFileSystemInfo fileSystemInfo)
    {
        // previously unpacked str2 directories have ".str2_pc.mode" extension
        return fileSystemInfo.Name.Contains(".str2_pc", StringComparison.OrdinalIgnoreCase);
    }

    public EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo)
    {
        if (fileSystemInfo is not IDirectoryInfo d)
        {
            throw new ArgumentException("Expected directory");
        }

        var match = Str2DirectoryNameFormat.Match(d.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Directory name is not in valid format: [{d.FullName}]");
        }

        var mode = Enum.Parse<RfgVpp.HeaderBlock.Mode>(match.Groups["mode"].Value, true);
        var name = match.Groups["name"].Value;
        var orderStr = match.Groups["order"].Value;
        var properties = new Properties();
        properties.Add(Properties.Mode, mode);
        if (!string.IsNullOrWhiteSpace(orderStr))
        {
            properties.Add(Properties.Index, int.Parse(orderStr));
        }

        return new EntryInfo(name, this, properties);
    }

    public string GetFileName(EntryInfo data)
    {
        var order = data.Properties.GetOrDefault<int?>(Properties.Index, null);
        var orderStr = order is null ? string.Empty : $"{order:D5} ";
        return $"{orderStr}{data.Name}";
    }

    public string GetDirectoryName(EntryInfo data)
    {
        var order = data.Properties.GetOrDefault<int?>(Properties.Index, null);
        var orderStr = order is null ? string.Empty : $"{order:D5} ";
        var mode = data.Properties.Get<RfgVpp.HeaderBlock.Mode>(Properties.Mode).ToString().ToLowerInvariant();
        return $"{orderStr}{data.Name}.{mode}";
    }

    /// <summary>
    /// Name format for unpacked str2 folders: order name.str2_pc.mode
    /// </summary>
    /// <example>misc.str2_pc.compressed</example>
    /// <example>00001 items.str2_pc.normal</example>
    public static readonly Regex Str2DirectoryNameFormat = new (@"^((?<order>\d+)\s+)?(?<name>(.*?).str2_pc)\.(?<mode>.*?)$", RegexOptions.Compiled);
}
