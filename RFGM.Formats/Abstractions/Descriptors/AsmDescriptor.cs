namespace RFGM.Formats.Abstractions.Descriptors;

/// <summary>
/// asset assembler files: asm_pc
/// </summary>
public class AsmDescriptor : FormatDescriptorBase
{
    public override bool IsPaired => false;
    public override bool IsContainer => false;
    protected override List<string> CanDecodeExt => [Extension];
    protected override List<string> CanEncodeExt => [JsonExtension];

    public static readonly string Extension = ".asm_pc";
    public static readonly string JsonExtension = ".asm_json";
}
