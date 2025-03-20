using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Player;

public class PlayerStatData //TODO: Implement
{
    public List<TrackedStat> Stats = new();

    [Flags]
    public enum TrackedStatFlags : int
    {
        None = 0,
        DisplayOnStatsScreen = 1 << 0,
        AllowUpdateByServer = 1 << 1,
        IsStoredInSaveGame = 1 << 2
    }

    public enum TrackedStatType : int
    {
        Int8 = 0x0,
        Int16 = 0x1,
        Int = 0x2,
        Float = 0x3,
        Distance = 0x4,
        Time = 0x5,
        Money = 0x6,
        Bool = 0x7,
        Percent = 0x8,
        PercentComplete = 0x9,
        MissionsCompleted = 0xA,
        WreckingCrewMapModeCombinationsPlayed = 0xB,
        WreckingCrewModesPlayed = 0xC
    }

    public struct TrackedStat
    {
        public string Value;
        public TrackedStatType ValueType;
        public TrackedStatFlags Flags;
        public bool IsActive;
    }

    public void Read(Stream stream)
    {
        throw new NotImplementedException();
            
        Stats = new List<TrackedStat>(639);

        for (int i = 0; i < Stats.Capacity; i++)
        {
            Stats.Add(new TrackedStat()
            {
                Value = stream.ReadAsciiNullTerminatedString(),
            });
        }
    }

}