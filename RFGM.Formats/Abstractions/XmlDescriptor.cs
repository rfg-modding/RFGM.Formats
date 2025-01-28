using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace RFGM.Formats.Abstractions;

/// <summary>
/// xml-like files: xtbl, dtdox, gtodx
/// </summary>
public class XmlDescriptor : FormatDescriptorBase
{
    public override bool IsPaired => false;
    public override bool IsContainer => false;
    protected override List<string> CanDecodeExt => [
        ".xtbl",
        ".dtdox",
        ".gtodx",
    ];

    protected override List<string> CanEncodeExt =>
    [
    ];

    protected override bool EncodeMatchInternal(IFileSystemInfo fileSystemInfo) => false;

    protected override EntryInfo FromFileSystemInternal(IFileSystemInfo fileSystemInfo)
    {
        throw new InvalidOperationException($"{Name}: Read xml files as regular files");
    }

    protected override string GetDecodeExt(string originalExt)
    {
        if (!CanDecodeExtensions.Contains(originalExt))
        {
            throw new ArgumentException($"{Name}: Invalid extension [{originalExt}]");
        }

        return Extension;
    }

    public static readonly string Extension = ".xml";

}
