using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public class LightClipObject
{
    public uint NameOffset;
    public Vector3 Bmin;
    public Vector3 Bmax;
    public Vector3 Pos;
    public Matrix3x3 Orient;
    public ushort SubmeshIndex;
    public ushort ObjectIdentifierIndex;

    public string Name = "";

    private const long SizeInFile = 88;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        NameOffset = stream.ReadUInt32();
        stream.Skip(4); //Padding
        Bmin = stream.ReadStruct<Vector3>();
        Bmax = stream.ReadStruct<Vector3>();
        Pos = stream.ReadStruct<Vector3>();
        Orient = stream.ReadStruct<Matrix3x3>();
        SubmeshIndex = stream.ReadUInt16();
        ObjectIdentifierIndex = stream.ReadUInt16();
        stream.Skip(4); //Padding
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for LightClipObject. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}