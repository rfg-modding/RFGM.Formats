using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Materials;

public struct TextureDesc()
{
    public uint NameOffset;
    public uint NameChecksum;
    public uint TextureIndex;
    
    //TODO: Figure out what hashing technique this is using. It appears to not use any of the ones currently in HashDictionary
    public string NameFromChecksum => HashDictionary.FindOriginString(NameChecksum) ?? "Unknown";

    public string Name = string.Empty;

    public void Read(Stream stream)
    {
        NameOffset = stream.ReadUInt32();
        NameChecksum = stream.ReadUInt32();
        TextureIndex = stream.ReadUInt32();
    }
}