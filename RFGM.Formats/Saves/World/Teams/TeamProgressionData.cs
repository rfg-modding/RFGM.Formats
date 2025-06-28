using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Teams;

public class TeamProgressionData
{
    public int EdfProgressionLevel;
    public float GuerrillaProgressionLevel;
    public int MarauderProgressionLevel;

    public void Read(Stream stream)
    {
        GuerrillaProgressionLevel = stream.ReadFloat();
        MarauderProgressionLevel = stream.ReadInt32();
        EdfProgressionLevel = stream.ReadInt32();
    }

    public void Write(Stream stream)
    {
        stream.WriteFloat(GuerrillaProgressionLevel);
        stream.WriteInt32(MarauderProgressionLevel);
        stream.WriteInt32(EdfProgressionLevel);
    }
}