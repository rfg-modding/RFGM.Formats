namespace RFGM.Formats.Vfx;

[Flags]
public enum VfxMeshType : uint
{
    None = 0,
    Normal = 1,
    Facing = 2,
    Rod = 4,
    Additive = 8,
    VolFog = 16,
}