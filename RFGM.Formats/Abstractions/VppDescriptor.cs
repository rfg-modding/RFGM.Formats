using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Abstractions;

public class VppDescriptor : IFormatDescriptor
{
    public Format Format => Format.Vpp;
    public bool CanDecode => true;
    public bool CanEncode => true;
    public bool IsPaired => false;
    public bool IsContainer => true;
    public ImmutableHashSet<string> Extensions { get; } = new HashSet<string>
    {
        ".vpp_pc"
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    public ImmutableHashSet<string> ConvertExtensions => ImmutableHashSet<string>.Empty;

    public bool Match(string name)
    {
        return name.EndsWith(".vpp_pc", StringComparison.OrdinalIgnoreCase);
    }

    public bool Match(IFileSystemInfo fileSystemInfo)
    {
        // previously unpacked vpp directories have ".vpp_pc.mode" extension
        return fileSystemInfo.Name.Contains(".vpp_pc", StringComparison.OrdinalIgnoreCase);
    }

    public EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo)
    {
        if (fileSystemInfo is not IDirectoryInfo d)
        {
            throw new ArgumentException("Expected directory");
        }

        var match = VppDirectoryNameFormat.Match(d.Name);
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
    /// Name format for unpacked vpp folders: order name.vpp_pc.mode
    /// </summary>
    /// <example>misc.vpp_pc.compressed</example>
    /// <example>00001 items.vpp_pc.normal</example>
    public static readonly Regex VppDirectoryNameFormat = new (@"^((?<order>\d+)\s+)?(?<name>(.*?).vpp_pc)\.(?<mode>.*?)$", RegexOptions.Compiled);
}
