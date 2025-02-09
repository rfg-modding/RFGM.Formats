namespace RFGM.Formats.Vfx;

[Flags]
public enum ParticleMaterialDataFlags : int
{
    Unused = 1,
    Additive = 2,
    Subtractive = 4,
    Distortion = 8,
    ColorPass = 16,
    HalfRes = 32,
    QuarterRes = 64,
    AlwaysOnTop = 128,
    SoftRender = 256,
    ColorCorrect = 512,
    GlowPass = 1024,
}