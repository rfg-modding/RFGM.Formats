using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public struct Cylinder
{
    public uint BodyPartId;
    public int ParentIndex;
    public Vector3 Axis;
    public Vector3 Position;
    public float Radius;
    public float Height;
    
    private const long SizeInFile = 40;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        BodyPartId = stream.ReadUInt32();
        ParentIndex = stream.ReadInt32();
        Axis = stream.ReadStruct<Vector3>();
        Position = stream.ReadStruct<Vector3>();
        Radius = stream.ReadFloat();
        Height = stream.ReadFloat();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Cylinder. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}