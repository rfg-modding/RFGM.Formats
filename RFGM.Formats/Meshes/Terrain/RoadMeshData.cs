using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct RoadMeshData
{
    public uint NumMeshInstances;
    public uint MaterialOffset;
    public uint MaterialHandle;
    public uint MaterialMapOffset;
    public uint MeshDataOffset;
    public uint MeshOffset;
    public uint RenderableOffset;
    public Vector3 Position;
    
    private const long SizeInFile = 40;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        NumMeshInstances = stream.ReadUInt32();
        MaterialOffset = stream.ReadUInt32();
        MaterialHandle = stream.ReadUInt32();
        MaterialMapOffset = stream.ReadUInt32();
        MeshDataOffset = stream.ReadUInt32();
        MeshOffset = stream.ReadUInt32();
        RenderableOffset = stream.ReadUInt32();
        Position = stream.ReadStruct<Vector3>();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RoadMeshData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}