namespace RFGM.Formats.Asset;

[Flags]
public enum PrimitiveFlags : byte
{
    None = 0,
    Loaded = 1, //Runtime flag. Set right after the primitive is successfully loaded.
    Flag1 = 2,
    Split = 4, //Primitive is split into cpu/gpu files
    Flag3 = 8,
    Flag4 = 16,
    Flag5 = 32,
    ReleaseError = 64, //Runtime flag. Set if stream2_container::req_release fails
    Flag7 = 128,
}