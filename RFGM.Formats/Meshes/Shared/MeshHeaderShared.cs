
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public struct MeshHeaderShared
{
    public uint Signature;
    public uint Version;
    public uint MeshOffset;
    //4 bytes padding
    public uint MaterialMapOffset;
    //4 bytes padding
    public uint MaterialsOffset;
    //4 bytes padding
    public uint NumMaterials;
    //4 bytes padding
    public uint TextureNamesOffset;
    //4 bytes padding

    public void Read(Stream stream)
    {
        Signature = stream.Read<uint>();
        Version = stream.Read<uint>();
        MeshOffset = stream.Read<uint>();
        stream.Skip(4);
        MaterialMapOffset = stream.Read<uint>();
        stream.Skip(4);
        MaterialsOffset = stream.Read<uint>();
        stream.Skip(4);
        NumMaterials = stream.Read<uint>();
        stream.Skip(4);
        TextureNamesOffset = stream.Read<uint>();
        stream.Skip(4);
    }
}