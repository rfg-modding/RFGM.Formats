using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public struct Sphere
{
    public uint BodyPartId;
    public int ParentIndex;
    public Vector3 Position;
    public float Radius;
    
    private const long SizeInFile = 24;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        BodyPartId = stream.ReadUInt32();
        ParentIndex = stream.ReadInt32();
        Position = stream.ReadStruct<Vector3>();
        Radius = stream.ReadFloat();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Sphere. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}