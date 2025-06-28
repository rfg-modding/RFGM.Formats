using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public class GeneralObject
{
    public uint NameOffset;
    public Vector3 Bmin;
    public Vector3 Bmax;
    public Vector3 InitialPos;
    public Matrix3x3 InitialOrient;
    public ushort[] CollisionModels = new ushort[2];
    public ushort Flags;
    public short[] MeshSubpieces = new short[2];

    public string Name = "";

    private const long SizeInFile = 96;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        NameOffset = stream.ReadUInt32();
        stream.Skip(4); //Padding
        Bmin = stream.ReadStruct<Vector3>();
        Bmax = stream.ReadStruct<Vector3>();
        InitialPos = stream.ReadStruct<Vector3>();
        InitialOrient = stream.ReadStruct<Matrix3x3>();
        CollisionModels[0] = stream.ReadUInt16();
        CollisionModels[1] = stream.ReadUInt16();
        Flags = stream.ReadUInt16();
        MeshSubpieces[0] = stream.ReadInt16();
        MeshSubpieces[1] = stream.ReadInt16();
        stream.Skip(2); //Padding
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