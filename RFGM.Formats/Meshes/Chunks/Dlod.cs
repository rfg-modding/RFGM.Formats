using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct Dlod
{
    public uint NameHash;
    public Vector3 Pos;
    public Matrix3x3 Orient;
    public ushort RenderSubpiece;
    public ushort FirstPiece;
    public byte MaxPieces;
    
    private const long SizeInFile = 60;

    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        NameHash = stream.ReadUInt32();
        Pos = stream.ReadStruct<Vector3>();
        Orient = stream.ReadStruct<Matrix3x3>();
        RenderSubpiece = stream.ReadUInt16();
        FirstPiece = stream.ReadUInt16();
        MaxPieces = stream.ReadUInt8();
        stream.Skip(3);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Dlod. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}