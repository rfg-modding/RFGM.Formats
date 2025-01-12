using RFGM.Formats.Hashes;

namespace RFGM.Formats.Zones;

public class RfgZoneObjectProperty(ushort type, ushort size, uint nameHash, byte[] data)
{
    public readonly ushort Type = type;
    public readonly ushort Size = size;
    public readonly uint NameHash = nameHash;
    public readonly byte[] Data = data;
        
    public string Name => HashDictionary.FindOriginString(NameHash) ?? "Unknown";
}