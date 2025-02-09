using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct RbbNode()
{
    public int NumObjects = 0;
    public RbbAabb Aabb = new();
    public int NodeDataOffset = 0;
}