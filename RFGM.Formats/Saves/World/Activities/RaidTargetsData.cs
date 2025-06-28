using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Activities;

public class RaidTargetsData
{
    public int ArraySize;
    public List<RaidTargetPair> Targets = new();

    public class RaidTargetPair
    {
        public uint RaidHandle;
        public uint TargetHandle;
    }

    public void Read(Stream stream)
    {
        ArraySize = stream.ReadInt32();
        var targetCount = stream.ReadInt32();
        for (var i = 0; i < targetCount; ++i)
        {
            Targets.Add(new RaidTargetPair
            {
                TargetHandle = stream.ReadUInt32(),
                RaidHandle = stream.ReadUInt32()
            });
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteInt32(ArraySize);
        stream.WriteInt32(Targets.Count);

        foreach (var target in Targets)
        {
            stream.WriteUInt32(target.TargetHandle);
            stream.WriteUInt32(target.RaidHandle);
        }
    }
}