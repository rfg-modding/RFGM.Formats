using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public struct TextureDesc
{
    public uint NameOffset;
    public uint NameChecksum;
    public uint TextureIndex;

    public void Read(Stream stream)
    {
        NameOffset = stream.ReadUInt32();
        NameChecksum = stream.ReadUInt32();
        TextureIndex = stream.ReadUInt32();
    }
}