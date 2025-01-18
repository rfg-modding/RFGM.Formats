using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct Subpiece
{
    public Vector3 Bmin;
    public Vector3 Bmax;
    public Vector3 Position;
    public Vector3 CenterOfMass;
    public float Mass;
    public uint DlodKey;
    public uint LinksOffset; //ushort offset
    public byte PhysicalMaterialIndex;
    public byte ShapeType;
    public byte NumLinks;
    public byte Flags;
    
    private const long SizeInFile = 64;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        Bmin = stream.ReadStruct<Vector3>();
        Bmax = stream.ReadStruct<Vector3>();
        Position = stream.ReadStruct<Vector3>();
        CenterOfMass = stream.ReadStruct<Vector3>();
        Mass = stream.ReadFloat();
        DlodKey = stream.ReadUInt32();
        LinksOffset = stream.ReadUInt32();
        PhysicalMaterialIndex = stream.ReadUInt8();
        ShapeType = stream.ReadUInt8();
        NumLinks = stream.ReadUInt8();
        Flags = stream.ReadUInt8();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Subpiece. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}