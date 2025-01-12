using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct TerrainStitchInfo
{
    public Vector2 Bmin;
    public Vector2 Bmax;
    public uint FilenameOffset;

    private const long SizeInFile = 20;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        Bmin = stream.ReadStruct<Vector2>();
        Bmax = stream.ReadStruct<Vector2>();
        FilenameOffset = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainStitchInfo. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}