using System.IO.Abstractions;

namespace RFGM.Formats.Abstractions.Descriptors;

/// <summary>
/// xml-like files: xtbl, dtdox, gtodx
/// </summary>
public class XmlDescriptor : FormatDescriptorBase
{
    public override bool IsPaired => false;
    public override bool IsContainer => false;
    protected override List<string> CanDecodeExt => [
        ".xtbl",
        ".dtodx",
        ".gtodx",
    ];

    protected override List<string> CanEncodeExt =>
    [
    ];

    protected override bool EncodeMatchInternal(string name) => false;

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
