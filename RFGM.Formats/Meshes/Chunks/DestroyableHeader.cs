using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct DestroyableHeader
{
    public uint AabbTreeOffset; //rfg_rbb_node offset
    public uint ObjectsOffset; //rfg_subpiece_base offset
    public uint ExtraDataOffset; //rfg_subpiece_base_extra_data offset
    public int NumObjects;
    public uint BaseLinksOffset; //rfg_links_base offset
    public int NumLinks;
    public uint DlodsOffset; //rfg_dlod_base offset
    public int NumDlods;
    public uint InstanceDataOffset; //rfg_destroyable_base_instance_data offset
    public uint TransformBufferOffset; //unsigned char buffer offset
    public float Mass;
    
    private const long SizeInFile = 44;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        AabbTreeOffset = stream.ReadUInt32();
        ObjectsOffset = stream.ReadUInt32();
        ExtraDataOffset = stream.ReadUInt32();
        NumObjects = stream.ReadInt32();
        BaseLinksOffset = stream.ReadUInt32();
        NumLinks = stream.ReadInt32();
        DlodsOffset = stream.ReadUInt32();
        NumDlods = stream.ReadInt32();
        InstanceDataOffset = stream.ReadUInt32();
        TransformBufferOffset = stream.ReadUInt32();
        Mass = stream.ReadFloat();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for DestroyableHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}