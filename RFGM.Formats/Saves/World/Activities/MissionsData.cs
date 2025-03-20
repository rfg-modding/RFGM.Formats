using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Activities;

public class MissionsData
{
    public MissionHandle CheckpointRestoreIndex;
    public Dictionary<string, MissionFlags> Missions = new();

    [Flags]
    public enum MissionFlags : ushort
    {
        None = 0,
        Complete = 1 << 0,
        LayersLoaded = 1 << 1,
        ScriptsLoaded = 1 << 2,
        CheckpointScriptLoaded = 1 << 3,
        Attempted = 1 << 4,
        Viewed = 1 << 5,
        Unlocked = 1 << 6,
        UnlockNotified = 1 << 7,
        Ready = 1 << 8,
        Checkpoint2ScriptLoaded = 1 << 9
    }

    public enum MissionHandle : uint
    {
        Invalid = 0xFFFFFFFF,
        ForceTo32Bit = 0x7FFFFFFF
    }

    public void Read(Stream stream)
    {
        CheckpointRestoreIndex = stream.ReadValueEnum<MissionHandle>();
        var missionCount = stream.ReadInt32();

        for (var i = 0; i < missionCount; ++i)
        {
            Missions.Add(stream.ReadLengthPrefixedString16(), stream.ReadValueEnum<MissionFlags>());
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteValueEnum<MissionHandle>(CheckpointRestoreIndex);
        stream.WriteInt32(Missions.Count);
        foreach (var mission in Missions)
        {
            stream.WriteLengthPrefixedString16(mission.Key);
            stream.WriteValueEnum<MissionFlags>(mission.Value);
        }
    }
}
