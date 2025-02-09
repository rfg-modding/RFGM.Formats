using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct DestroyableInstanceData
{
    public uint ObjectsOffset;
    public uint LinksOffset;
    public uint DlodsOffset;
    public uint DataSize;
    public uint BufferOffset; //et_ptr_offset<unsigned char, 0> buffer;

    public byte[] Data = [];
    
    private const long SizeInFile = 20;

    public DestroyableInstanceData()
    {
        
    }
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif
        
        ObjectsOffset = stream.ReadUInt32();
        LinksOffset = stream.ReadUInt32();
        DlodsOffset = stream.ReadUInt32();
        DataSize = stream.ReadUInt32();
        BufferOffset = stream.ReadUInt32();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for DestroyableInstanceData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif

        if (DataSize > 0)
        {
            Data = new byte[DataSize];
            stream.ReadExactly(Data);
        }
    }
}