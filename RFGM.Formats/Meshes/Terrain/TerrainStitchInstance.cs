using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct TerrainStitchInstance
{
    public uint StitchChunkNameOffset;
    public uint NumSkirts;
    public uint SkirtsOffset;
    public uint NumStitchedSkirts;
    public uint StitchedSkirtsOffset;
    public Vector3 Position;
    public Matrix3x3 Rotation;
    public uint HavokHandle;
    //32 bytes padding
    
    private const long SizeInFile = 104;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        StitchChunkNameOffset = stream.ReadUInt32();
        NumSkirts = stream.ReadUInt32();
        SkirtsOffset = stream.ReadUInt32();
        NumStitchedSkirts = stream.ReadUInt32();
        StitchedSkirtsOffset = stream.ReadUInt32();
        Position = stream.ReadStruct<Vector3>();
        Rotation = stream.ReadStruct<Matrix3x3>();
        HavokHandle = stream.ReadUInt32();
        stream.Skip(32);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainStitchInstance. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}