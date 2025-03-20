using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Player;

public class PlayerMetadata
{
    public int Salvage;
    public int MiningCount;
    public int SupplyCrateCount;
    public PlayerUpgrades UpgradeData = new();
    public PlayerDifficulty Difficulty = new();
    public uint DistrictHash;
    public int DistrictTime;
    public int PlayTime;
    public int LastDeathTime;

    public void Read(Stream stream, int dlcId)
    {
        Salvage = stream.ReadInt32();
        MiningCount = stream.ReadInt32();
        SupplyCrateCount = stream.ReadInt32();
        UpgradeData.Read(stream, dlcId);
        Difficulty.Read(stream);
        DistrictHash = stream.ReadUInt32();
        DistrictTime = stream.ReadInt32();
        PlayTime = stream.ReadInt32();
        LastDeathTime = stream.ReadInt32();
    }

    public void Write(Stream stream)
    {
        stream.WriteInt32(Salvage);
        stream.WriteInt32(MiningCount);
        stream.WriteInt32(SupplyCrateCount);
        UpgradeData.Write(stream);
        Difficulty.Write(stream);
        stream.WriteUInt32(DistrictHash);
        stream.WriteInt32(DistrictTime);
        stream.WriteInt32(PlayTime);
        stream.WriteInt32(LastDeathTime);
    }

    public class PlayerUpgrades //TODO: Refactor
    {
        public int Marker;
        public List<UpgradeItem> Upgrades = new();

        public class UpgradeItem
        {
            public byte CurrentLevel;
            public ushort AvailabilityBitfield;
            public ushort NewNotifiedBitfield;
            public ushort UnlockedNotifiedBitfield;
        }

        public void Read(Stream stream, int dlcId)
        {
            Marker = stream.ReadInt32();
            if (Marker == -1)
            {
                using MemoryStream ms = new(stream.ReadBytes(0x400));
                for (var i = 0; i < 128; i++)
                {
                    var item = new UpgradeItem();

                    item.CurrentLevel = ms.ReadUInt8();
                    ms.Skip(1);
                    item.AvailabilityBitfield = ms.ReadUInt16();
                    item.UnlockedNotifiedBitfield = ms.ReadUInt16();
                    item.NewNotifiedBitfield = ms.ReadUInt16();

                    Upgrades.Add(item);
                }
            }
            else
            {
                stream.Seek(-4, SeekOrigin.Current);
                var count = dlcId != -1 ? 39 : 32;
                for (var i = 0; i < count; i++)
                {
                    var item = new UpgradeItem();

                    item.CurrentLevel = stream.ReadUInt8();
                    stream.Skip(1);
                    item.AvailabilityBitfield = stream.ReadUInt16();
                    item.UnlockedNotifiedBitfield = stream.ReadUInt16();
                    item.NewNotifiedBitfield = stream.ReadUInt16();

                    Upgrades.Add(item);
                }
            }
        }

        public void Write(Stream stream)
        {
            if (Marker == -1)
            {
                stream.WriteInt32(-1);
            }

            foreach (var t in Upgrades)
            {
                stream.WriteUInt8(t.CurrentLevel);
                stream.Skip(1);
                stream.WriteUInt16(t.AvailabilityBitfield);
                stream.WriteUInt16(t.UnlockedNotifiedBitfield);
                stream.WriteUInt16(t.NewNotifiedBitfield);
            }
        }
    }

    public class PlayerDifficulty //TODO: Refactor
    {
        private const int DifficultyValueCount = 27;
        private const int DifficultyLevels = 5;

        public DifficultyLevel CurrentDifficulty;
        public float[,] DifficultyValues;

        public enum DifficultyLevel : uint
        {
            Invalid = 0xFFFFFFFF,
            Easy = 0x0,
            Medium = 0x1,
            Hard = 0x2,
            Insane = 0x3,
            Custom = 0x4
        }

        public void Read(Stream stream)
        {
            CurrentDifficulty = stream.ReadValueEnum<DifficultyLevel>();
            var valueCount = stream.ReadInt32();

            DifficultyValues = new float[valueCount, DifficultyLevels];

            switch (valueCount)
            {
                case DifficultyValueCount:
                {
                    for (var i = 0; i < DifficultyValueCount; i++)
                    {
                        DifficultyValues[i, 4] = stream.ReadFloat();
                    }

                    break;
                }
                case > 0:
                {
                    for (var i = 0; i < valueCount; i++)
                    {
                        stream.Skip(4);
                    }

                    break;
                }
            }
        }

        public void Write(Stream stream)
        {
            stream.WriteValueEnum<DifficultyLevel>(CurrentDifficulty);
            stream.WriteUInt32(DifficultyValueCount);

            for (var i = 0; i < DifficultyValueCount; i++)
            {
                stream.WriteFloat(DifficultyValues[i, 4]);
            }
        }

    }
}