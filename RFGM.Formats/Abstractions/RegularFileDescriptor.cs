using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace RFGM.Formats.Abstractions;

public class RegularFileDescriptor : IFormatDescriptor
{
    public Format Format => Format.RegularFile;
    public bool CanDecode => true;
    public bool CanEncode => true;
    public bool IsPaired => false;
    public bool IsContainer => false;
    public ImmutableHashSet<string> Extensions => ImmutableHashSet<string>.Empty;

    public bool Match(string name)
    {
        return false;
    }

    public bool Match(IFileSystemInfo fileSystemInfo)
    {
        return false;
    }

    public EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo)
    {
        if (fileSystemInfo is not IFileInfo f)
        {
            throw new ArgumentException("Expected file");
        }

        var match = RegularFileNameFormat.Match(f.Name);
        if (!match.Success)
        {
            throw new InvalidOperationException($"File name is not in valid format: [{f.FullName}]");
        }

        var name = match.Groups["name"].Value;
        var orderStr = match.Groups["order"].Value;
        var properties = new Properties();
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
        throw new InvalidOperationException("Not a container");
    }

    /// <summary>
    /// Name format for regular files: order name
    /// </summary>
    /// <example>table.xtbl</example>
    /// <example>00001 player.xtbl</example>
    public static readonly Regex RegularFileNameFormat = new (@"^((?<order>\d+)\s+)?(?<name>(.*?))$", RegexOptions.Compiled);
}
