using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public struct ChunkFileHeader()
{
    public uint Signature = 0;
    public uint Version = 0;
    public uint SourceVersion = 0;
    public uint RenderDataChecksum = 0;
    public uint RenderCpuDataOffset = 0;
    public uint RenderCpuDataSize = 0;
    public uint CollisionModelChecksum = 0;
    public uint CollisionModelDataOffset = 0;
    public uint CollisionModelDataSize = 0;
    public uint DestructionChecksum = 0;
    public uint DestructionOffset = 0;
    public uint DestructionDataSize = 0;

    private const long SizeInFile = 48;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        Signature = stream.ReadUInt32();
        Version = stream.ReadUInt32();
        SourceVersion = stream.ReadUInt32();
        RenderDataChecksum = stream.ReadUInt32();
        RenderCpuDataOffset = stream.ReadUInt32();
        RenderCpuDataSize = stream.ReadUInt32();
        CollisionModelChecksum = stream.ReadUInt32();
        CollisionModelDataOffset = stream.ReadUInt32();
        CollisionModelDataSize = stream.ReadUInt32();
        DestructionChecksum = stream.ReadUInt32();
        DestructionOffset = stream.ReadUInt32();
        DestructionDataSize = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ChunkFileHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}