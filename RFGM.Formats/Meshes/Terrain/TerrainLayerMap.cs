using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct TerrainLayerMap
{
    public uint ResX;
    public uint ResY;
    public uint BitDepth;
    public uint DataSize;
    public uint DataOffset;
    public uint NumMaterials;
    public uint MaterialNamesOffset;
    public uint MaterialIndexOffset;
    
    private const long SizeInFile = 32;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        ResX = stream.ReadUInt32();
        ResY = stream.ReadUInt32();
        BitDepth = stream.ReadUInt32();
        DataSize = stream.ReadUInt32();
        DataOffset = stream.ReadUInt32();
        NumMaterials = stream.ReadUInt32();
        MaterialNamesOffset = stream.ReadUInt32();
        MaterialIndexOffset = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainLayerMap. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}