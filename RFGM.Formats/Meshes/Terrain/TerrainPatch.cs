using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct TerrainPatch
{
    public uint InstanceOffset;
    public Vector3 Position;
    public Matrix3x3 Rotation;
    public uint SubmeshIndex;
    public BoundingBox LocalAabb;
    public Vector3 LocalBspherePosition;
    public float LocalBsphereRadius;
    
    private const long SizeInFile = 96;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        InstanceOffset = stream.ReadUInt32();
        Position = stream.ReadStruct<Vector3>();
        Rotation = stream.ReadStruct<Matrix3x3>();
        SubmeshIndex = stream.ReadUInt32();
        LocalAabb = stream.ReadStruct<BoundingBox>();
        LocalBspherePosition = stream.ReadStruct<Vector3>();
        LocalBsphereRadius = stream.ReadFloat();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainPatch. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}