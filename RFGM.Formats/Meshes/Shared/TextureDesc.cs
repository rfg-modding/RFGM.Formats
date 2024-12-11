using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public struct TextureDesc
{
    public uint NameOffset;
    public uint NameChecksum;
    public uint TextureIndex;

    public void Read(Stream stream)
    {
        NameOffset = stream.Read<uint>();
        NameChecksum = stream.Read<uint>();
        TextureIndex = stream.Read<uint>();
    }
}