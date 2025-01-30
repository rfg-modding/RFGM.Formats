using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct RbbNode()
{
    public int NumObjects = 0;
    public RbbAabb Aabb = new();
    public uint NodeDataOffset = 0; //et_ptr_offset<unsigned char, 0> node_data;
    
    private const long SizeInFile = 20;

    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif
        
        NumObjects = stream.ReadInt32();
        Aabb.Read(stream);
        NodeDataOffset = stream.ReadUInt32();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RbbNode. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}