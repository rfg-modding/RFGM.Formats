using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Territory;

public class DistrictData
{
    public List<District> Districts = new();

    public class District
    {
        public float Control;
        public float ControlMax;
        public bool Liberated;
        public float Morale;
        public float MoraleMax;
        public bool NeedsToPlayRadioLine;
    }

    public void Read(Stream stream)
    {
        var districtCount = stream.ReadInt32();
        for (var i = 0; i < districtCount; i++)
        {
            Districts.Add(new District
            {
                ControlMax = stream.ReadFloat(),
                Control = stream.ReadFloat(),
                MoraleMax = stream.ReadFloat(),
                Morale = stream.ReadFloat(),
                Liberated = stream.ReadBoolean(),
                NeedsToPlayRadioLine = stream.ReadBoolean()
            });
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteInt32(Districts.Count);
        foreach (var district in Districts)
        {
            stream.WriteFloat(district.ControlMax);
            stream.WriteFloat(district.Control);
            stream.WriteFloat(district.MoraleMax);
            stream.WriteFloat(district.Morale);
            stream.WriteBoolean(district.Liberated);
            stream.WriteBoolean(district.NeedsToPlayRadioLine);
        }
    }

}