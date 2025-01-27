using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace RFGM.Formats.Abstractions;

public class PegDescriptor : IFormatDescriptor
{
    public Format Format => Format.Peg;
    public bool CanDecode => true;
    public bool CanEncode => true;
    public bool IsPaired => true;
    public bool IsContainer => true;

    public ImmutableHashSet<string> Extensions { get; } = new HashSet<string>
    {
        ".cvbm_pc",
        ".gvbm_pc",
        ".cpeg_pc",
        ".gpeg_pc",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    public bool Match(string name)
    {
        return Extensions.Contains(FormatUtils.GetFullExtension(name));
    }

    public bool Match(IFileSystemInfo fileSystemInfo)
    {
        // previously unpacked peg directories have ".cpeg_pc.align" extension
        return Extensions.Any(x => fileSystemInfo.Name.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    public EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo)
    {
        if (fileSystemInfo is not IDirectoryInfo d)
        {
            throw new ArgumentException("Expected directory");
        }

        var match = PegDirectoryNameFormat.Match(d.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Directory name is not in valid format: [{d.FullName}]");
        }

        var align = int.Parse(match.Groups["align"].Value);
        var name = match.Groups["name"].Value;
        var orderStr = match.Groups["order"].Value;
        var properties = new Properties();
        properties.Add(Properties.Align, align);
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
        var mode = data.Properties.Get<int>(Properties.Align).ToString().ToLowerInvariant();
        return $"{orderStr}{data.Name}.{mode}";
    }

    /// <summary>
    /// Name format for unpacked peg folders: name.cpeg_pc.align
    /// </summary>
    /// <example>new_jetpack.cpeg_pc.16</example>
    /// <example>items_containers.cpeg_pc.256</example>
    public static readonly Regex PegDirectoryNameFormat = new (@"^((?<order>\d+)\s+)?(?<name>.*?)\.(?<align>\d+?)$", RegexOptions.Compiled);
}
