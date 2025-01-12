using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public struct TextureDesc
{
    public uint NameOffset;
    public uint NameChecksum;
    public uint TextureIndex;
    
    public string Name => HashDictionary.FindOriginString(NameChecksum) ?? "Unknown";

    public void Read(Stream stream)
    {
        NameOffset = stream.ReadUInt32();
        NameChecksum = stream.ReadUInt32();
        TextureIndex = stream.ReadUInt32();
    }
}