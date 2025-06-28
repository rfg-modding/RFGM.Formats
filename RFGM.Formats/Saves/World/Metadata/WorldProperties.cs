using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Metadata;

public class WorldProperties
{
    public float TechLevel;
    public float TechLevelMax;
    public bool AllowWalkerSpawn;

    public void Read(Stream stream)
    {
        TechLevel = stream.ReadFloat();
        TechLevelMax = stream.ReadFloat();
        AllowWalkerSpawn = stream.ReadBoolean8();
    }

    public void Write(Stream stream)
    {
        stream.WriteFloat(TechLevel);
        stream.WriteFloat(TechLevelMax);
        stream.WriteBoolean8(AllowWalkerSpawn);
    }
}