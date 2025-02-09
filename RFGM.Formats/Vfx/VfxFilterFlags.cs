namespace RFGM.Formats.Vfx;

[Flags]
public enum VfxFilterFlags : int
{
    Global = 0,
    PerEmitter = 1,
    PerEffect = 2,
}