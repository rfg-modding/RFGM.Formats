using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct SubpieceData
{
    public uint ShapeOffset; //havok shape offset
    public ushort CollisionModel;
    public ushort RenderSubpiece;
    public uint PhysicalMaterialNameChecksum;
    
    public string PhysicalMaterialName => HashDictionary.FindOriginString(PhysicalMaterialNameChecksum) ?? "Unknown";
    
    private const long SizeInFile = 12;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        ShapeOffset = stream.ReadUInt32();
        CollisionModel = stream.ReadUInt16();
        RenderSubpiece = stream.ReadUInt16();
        PhysicalMaterialNameChecksum = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for SubpieceData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}