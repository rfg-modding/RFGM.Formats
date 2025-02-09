namespace RFGM.Formats.Meshes.Chunks;

public struct Destroyable()
{
    public DestroyableHeader Header;

    public List<Subpiece> Subpieces = new();
    public List<SubpieceData> SubpieceData = new();
    public List<Link> Links = new();
    public List<Dlod> Dlods = new();
    public List<RbbNode> RbbNodes = new();
    public DestroyableInstanceData InstanceData = new();
}