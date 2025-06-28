namespace RFGM.Formats.Materials;

[Flags]
public enum RfgMaterialFlags : uint
{
    None = 0,
    Transparent = 1,
    Flag2 = 2,
    Flag4 = 4,
    Flag8 = 8,
    ShaderHandleSet = 16, //Runtime only value. Set when the ShaderHandle field is set.
    Flag32 = 32,
    NoCulling = 64,       //In rl_material::set_material() in the game sets the shader cull state to RL_CULL_NONE if this flag is not set. Otherwise set to RL_CULL_CCW.
    Flag128 = 128,
    ConstantDataSet = 256, //The game sets this in rl_material::set_constant_data(). Might be runtime only. I haven't seen it in any of the mesh files yet.
    Flag512 = 512,
    Flag1024 = 1024,
    Flag2048 = 2048,
    Flag4096 = 4096,
    Flag8192 = 8192,
    Flag16384 = 16384,
    Flag32768 = 32768,
    Flag65536 = 65536,
    //NOTE: Only stopped here defined flags since it appears the game does not use the rest
}