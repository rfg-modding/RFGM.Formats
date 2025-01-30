namespace RFGM.Formats.Abstractions.Descriptors;

/// <summary>
/// just a file without conversions or magic
/// </summary>
public class RegularFileDescriptor : FormatDescriptorBase
{
    /// <summary>
    /// always true regardless of extensions: we can copy any file
    /// </summary>
    public override bool CanDecode => true;

    /// <summary>
    /// always true regardless of extensions: we can copy any file
    /// </summary>
    public override bool CanEncode => true;
    public override bool IsPaired => false;
    public override bool IsContainer => false;
    protected override List<string> CanDecodeExt => [];
    protected override List<string> CanEncodeExt => [];

    /// <summary>
    /// Should return false to avoid matching with any files. This descriptor is selected manually as last resort
    /// </summary>
    protected override bool DecodeMatchInternal(string name) => false;

    /// <summary>
    /// Should return false to avoid matching with any files. This descriptor is selected manually as last resort
    /// </summary>
    protected override bool EncodeMatchInternal(string name) => false;

    protected override string GetDecodeExt(string originalExt) => originalExt;

    protected override string GetEncodeExt(string originalExt) => originalExt;
}
