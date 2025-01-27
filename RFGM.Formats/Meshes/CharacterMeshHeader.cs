using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public struct CharacterMeshHeader
{
    public MeshHeaderShared Shared;
        
    public uint NumLods;
    //4 bytes padding
        
    public int LodSubmeshIdOffset;
    //4 bytes padding
        
    public int NumBones;
    //4 bytes padding
        
    public uint BoneIndicesOffset;
    //4 bytes padding

    public short NumSpheres;
    public short NumCylinders;
    //4 bytes padding

    public uint SpheresOffset;
    //4 bytes padding
        
    public uint CylindersOffset;
    //4 bytes padding
    
    private const long SizeInFile = 104;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif
        
        Shared.Read(stream);
        NumLods = stream.ReadUInt32();
        stream.Skip(4);
        LodSubmeshIdOffset = stream.ReadInt32();
        stream.Skip(4);
        NumBones = stream.ReadInt32();
        stream.Skip(4);
        BoneIndicesOffset = stream.ReadUInt32();
        stream.Skip(4);
        NumSpheres = stream.ReadInt16();
        NumCylinders = stream.ReadInt16();
        stream.Skip(4);
        SpheresOffset = stream.ReadUInt32();
        stream.Skip(4);
        CylindersOffset = stream.ReadUInt32();
        stream.Skip(4);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for CharacterMeshHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}