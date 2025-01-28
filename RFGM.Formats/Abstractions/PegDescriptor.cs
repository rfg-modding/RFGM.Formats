using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using RFGM.Formats.Peg;

namespace RFGM.Formats.Abstractions;

/// <summary>
/// texture containers: cpeg_pc+gpeg_pc and cvbm_pc+gvbm_pc
/// </summary>
public class PegDescriptor : FormatDescriptorBase
{
    public override bool IsPaired => true;
    public override bool IsContainer => true;
    protected override List<string> CanDecodeExt => [
        ".cvbm_pc",
        ".gvbm_pc",
        ".cpeg_pc",
        ".gpeg_pc",];
    protected override List<string> CanEncodeExt => CanDecodeExt;

    protected override string GetDecodeExt(string originalExt)
    {
        if (!CanDecodeExtensions.Contains(originalExt))
        {
            throw new ArgumentException($"{Name}: Invalid extension [{originalExt}]");
        }

        return originalExt;
    }

    protected override string GetEncodeExt(string originalExt)
    {
        if (!CanEncodeExtensions.Contains(originalExt))
        {
            throw new ArgumentException($"{Name}: Invalid extension [{originalExt}]");
        }

        return originalExt;
    }
}
