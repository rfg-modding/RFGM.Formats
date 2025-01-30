using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct TerrainRenderableData
{
    public uint MeshDataOffset;
    public uint RenderableOffset;
    public BoundingBox Aabb;
    public Vector3 BspherePosition;
    public float BsphereRadius;
    
    private const long SizeInFile = 48;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        MeshDataOffset = stream.ReadUInt32();
        RenderableOffset = stream.ReadUInt32();
        Aabb = stream.ReadStruct<BoundingBox>();
        BspherePosition = stream.ReadStruct<Vector3>();
        BsphereRadius = stream.ReadFloat();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainRenderableData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}