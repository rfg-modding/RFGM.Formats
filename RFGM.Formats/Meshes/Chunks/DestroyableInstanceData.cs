using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct DestroyableInstanceData
{
    public uint ObjectsOffset;
    public uint LinksOffset;
    public uint DlodsOffset;
    public uint DataSize;
    public uint BufferOffset; //et_ptr_offset<unsigned char, 0> buffer;
    
    private const long SizeInFile = 20;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        ObjectsOffset = stream.ReadUInt32();
        LinksOffset = stream.ReadUInt32();
        DlodsOffset = stream.ReadUInt32();
        DataSize = stream.ReadUInt32();
        BufferOffset = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for DestroyableInstanceData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}