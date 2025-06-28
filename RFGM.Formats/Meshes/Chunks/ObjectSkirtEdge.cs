using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public class ObjectSkirtEdge
{
    public Vector3[] Points = new Vector3[2];
    public Vector3[] Normals = new Vector3[2];

    private const long SizeInFile = 48;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        Points[0] = stream.ReadStruct<Vector3>();
        Points[1] = stream.ReadStruct<Vector3>();
        Normals[0] = stream.ReadStruct<Vector3>();
        Normals[1] = stream.ReadStruct<Vector3>();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ObjectSkirtEdge. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}