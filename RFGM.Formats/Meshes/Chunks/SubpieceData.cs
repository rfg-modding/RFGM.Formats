using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct SubpieceData
{
    public uint ShapeOffset; //havok shape offset
    public ushort CollisionModel;
    public ushort RenderSubpiece;
    public uint Unknown0;
    
    private const long SizeInFile = 12;

    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        ShapeOffset = stream.ReadUInt32();
        CollisionModel = stream.ReadUInt16();
        RenderSubpiece = stream.ReadUInt16();
        Unknown0 = stream.ReadUInt32();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for SubpieceData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}