using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.VIP;

public class VipData
{
    public List<string> RestrictedHumans = new();
        
    public void Read(Stream stream)
    {
        var humanCount = stream.ReadUInt32();
            
        for (var i = 0; humanCount > i; ++i)
        {
            RestrictedHumans.Add(stream.ReadLengthPrefixedString16());
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32((uint)RestrictedHumans.Count);
        foreach (var human in RestrictedHumans)
        {
            stream.WriteLengthPrefixedString16(human);
        }
    }
}