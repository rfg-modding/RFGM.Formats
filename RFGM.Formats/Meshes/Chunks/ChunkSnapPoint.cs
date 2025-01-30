using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct ChunkSnapPoint
{
    public Matrix3x3 Orient;
    public Vector3 Position;
    
    private const long SizeInFile = 48;

    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        Orient = stream.ReadStruct<Matrix3x3>();
        Position = stream.ReadStruct<Vector3>();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ChunkSnapPoint. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}