namespace RFGM.Formats.Meshes.Chunks;

public struct Destroyable()
{
    public DestroyableHeader Header;

    public List<Subpiece> Subpieces = new();
    public List<SubpieceData> SubpieceData = new();
    public List<Link> Links = new();
    public List<Dlod> Dlods = new();
    
    //TODO: Fix
    //Note: These aren't read yet. Chunk format hasn't been 100% reversed yet.
    public List<RbbNode> RbbNodes = new();
    public DestroyableInstanceData InstanceData = new();
    
    //Additional data stored in a separate part of the chunk file
    public uint UID = uint.MaxValue;
    public string Name = string.Empty;
    public uint IsDestroyable = 0;
    public uint NumSnapPoints = 0;
    public ChunkSnapPoint[] SnapPoints = new ChunkSnapPoint[10];
}