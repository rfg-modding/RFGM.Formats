using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Territory;

public class ZoneData
{
    public List<WorldZone> Zones = new();

    public class WorldZone
    {
        public string Name;
        public int CurrentSize;
        public int MaxSize;
        public byte[] Buffer;
    }

    public void Read(Stream stream, uint territoryZonesCount)
    {
        for (var i = 0; i < territoryZonesCount; i++)
        {
            WorldZone zone = new();
            
            zone.Name = stream.ReadLengthPrefixedString16();
            stream.Align(4);
            zone.CurrentSize = stream.ReadInt32();
            zone.MaxSize = stream.ReadInt32();

            if (zone.CurrentSize > 0)
            {
                zone.Buffer = new byte[zone.CurrentSize];
                stream.ReadExactly(zone.Buffer, 0, zone.CurrentSize);
                stream.Align(4);
            }

            Zones.Add(zone);
        }
    }

    public void Write(Stream stream)
    {
        foreach (var zone in Zones)
        {
            stream.WriteLengthPrefixedString16(zone.Name);
            stream.Align(4);
            stream.WriteInt32(zone.CurrentSize);
            stream.WriteInt32(zone.MaxSize);
            
            if (zone.CurrentSize <= 0) continue;
            
            stream.Write(zone.Buffer, 0, zone.CurrentSize);
            stream.Align(4);
        }
    }
}