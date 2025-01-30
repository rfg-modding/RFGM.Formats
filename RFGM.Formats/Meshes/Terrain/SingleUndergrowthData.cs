using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct SingleUndergrowthData
{
    public uint MeshIndex;
    public Vector3 Position;
    public float Scale;
    public float ColorLerp;
    
    private const long SizeInFile = 24;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif
      
        MeshIndex = stream.ReadUInt32();
        Position = stream.ReadStruct<Vector3>();
        Scale = stream.ReadFloat();
        ColorLerp = stream.ReadFloat();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for SingleUndergrowthData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}