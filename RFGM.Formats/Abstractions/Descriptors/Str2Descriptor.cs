namespace RFGM.Formats.Abstractions.Descriptors;

/// <summary>
/// str2_pc archives
/// </summary>
public class Str2Descriptor : FormatDescriptorBase
{
    public override bool IsPaired => false;
    public override bool IsContainer => true;
    protected override List<string> CanDecodeExt => [Extension];
    protected override List<string> CanEncodeExt => CanDecodeExt;

    public static readonly string Extension = ".str2_pc";
}
