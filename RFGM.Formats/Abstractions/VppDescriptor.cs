using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Abstractions;

/// <summary>
/// vpp_pc archives (like str2, with asm metadata)
/// </summary>
public class VppDescriptor : FormatDescriptorBase
{
    public override bool IsPaired => false;
    public override bool IsContainer => true;
    protected override List<string> CanDecodeExt => [Extension];
    protected override List<string> CanEncodeExt => CanDecodeExt;

    public static readonly string Extension = ".vpp_pc";
}
