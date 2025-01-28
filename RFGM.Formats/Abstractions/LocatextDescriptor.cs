using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace RFGM.Formats.Abstractions;

/// <summary>
/// localization files: rfglocatext
/// </summary>
public class LocatextDescriptor : FormatDescriptorBase
{
    public override bool IsPaired => false;
    public override bool IsContainer => false;
    protected override List<string> CanDecodeExt => [".rfglocatext"];
    protected override List<string> CanEncodeExt => [".rfglocatext.xml"];
}
