namespace RFGM.Formats.Asset;

public enum AllocatorType : byte
{
    None = 0,
    World = 1,
    ChunkPreload = 2,
    EffectPreload = 3,
    EffectCutscene = 4,
    ItemPreload = 5,
    DecalPreload = 6,
    ClothSimPreload = 7,
    Tod = 8,
    MpEffectPreload = 9,
    MpItemPreload = 10,
    Player = 11,
    Human = 12,
    LargeWeapon = 13,
    SmallWeapon = 14,
    Vehicle = 15,
    LargeLayer = 16,
    SmallLayer = 17,
    HumanVoicePersona = 18,
    AlwaysLoadedHumanVoicePersona = 19,
    Audio = 20,
    Interface = 21,
    Fsm = 22,
    InterfaceStack = 23,
    InterfaceSlot = 24,
    InterfaceMpPreload = 25,
    InterfaceMpSlot = 26,
    MaterialEffect = 27,
    Permanent = 28,
    DlcEffectPreload = 29,
    DlcItemPreload = 30,
    NumAllocatorTypes = 31,
}

public enum ContainerType : byte
{
    None = 0,
    Glass = 1,
    EffectsEnv = 2,
    EffectsPreload = 3,
    EffectsDlc = 4,
    MpEffects = 5,
    LayerSmall = 6,
    LayerLarge = 7,
    Audio = 8,
    ClothSim = 9,
    Decals = 10,
    DecalsPreload = 11,
    Fsm = 12,
    Ui = 13,
    Env = 14,
    Chunk = 15,
    ChunkPreload = 16,
    Stitch = 17,
    World = 18,
    HumanHead = 19,
    Human = 20,
    Player = 21,
    Items = 22,
    ItemsPreload = 23,
    ItemsMpPreload = 24,
    ItemsDlc = 25,
    WeaponLarge = 26,
    WeaponSmall = 27,
    Skybox = 28,
    Vehicle = 29,
    VoicePersona = 30,
    AlwaysLoadedVoicePersona = 31,
    Foliage = 32,
    UiPeg = 33,
    MaterialEffect = 34,
    MaterialPreload = 35,
    SharedBackpack = 36,
    LandmarkLod = 37,
    GpsPreload = 38,
    NumContainerTypes = 39
}

public enum PrimitiveType : byte
{
    None = 0,
    Peg = 1,
    Chunk = 2,
    Zone = 3,
    Terrain = 4,
    StaticMesh = 5,
    CharacterMesh = 6,
    FoliageMesh = 7,
    Material = 8,
    ClothSim = 9,
    Vehicle = 10,
    VehicleAudio = 11,
    Vfx = 12,
    Wavebank = 13,
    FoleyBank = 14,
    MeshMorph = 15,
    VoicePersona = 16,
    AnimFile = 17,
    Vdoc = 18,
    LuaScript = 19,
    Localization = 20,
    TerrainHighLod = 21,
    LandmarkLod = 22,
    NumPrimitiveTypes = 23,
}

[Flags]
public enum ContainerFlags : ushort
{
    None = 0,
    Loaded = 1, //Runtime flag. Set right after the container is loaded
    Flag1 = 2,
    Flag2 = 4,
    Flag3 = 8, //Possibly a runtime only flag that means the container + primitives have been read into memory. Not yet confirmed.
    Flag4 = 16,
    Flag5 = 32,
    ReleaseError = 64, //Runtime flag. Set if stream2_container::req_release fails
    //If set then the container will have a list of primitive sizes before the list of primitives. These are duplicates of the HeaderSize and DataSize in each primitive.
    //This might also be the way to indicate whether the container is a str2_pc or a "virtual" container
    HasSizeList = 128, 
    Passive = 256, //If it's true the container is placed into the passive stream queue. It's unknown what "passive" means in this case.
    Flag9 = 512,
    Flag10 = 1024,
    Flag11 = 2048,
    Flag12 = 4096,
    Flag13 = 8192,
    Flag14 = 16384,
    Flag15 = 32768,
}

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