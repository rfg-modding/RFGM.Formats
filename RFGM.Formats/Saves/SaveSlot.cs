using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves;

public class SaveSlot
{
    private const int SlotSizeBytes = 2621652 + SlotPaddingBytes;
    private const int SlotPaddingBytes = 1024;
    private const int WorldSizeBytes = 2621456;

    public GameSaveInfo Info = new();
    public SaveWorld World = new();
    public bool Exists;
    public int SizeUsed;

    public void Read(Stream stream)
    {
        Exists = stream.ReadBoolean32();
        if (!Exists)
        {
            stream.Skip(SlotSizeBytes - 0x4);
            return;
        }

        Info.Read(stream);
        SizeUsed = stream.ReadInt32();
        using (var ms = new MemoryStream(stream.ReadBytes(WorldSizeBytes)))
        {
            World.Read(ms, Info.DlcId);
        }

        stream.Skip(SlotPaddingBytes);
    }

    public void Write(Stream stream)
    {
        stream.WriteBoolean32(Exists);
        if (!Exists)
        {
            stream.Skip(SlotSizeBytes - 0x4);
            return;
        }

        Info.Write(stream);

        var sizeUsedPos = stream.Position;
        stream.WriteInt32(0);

        var worldBuffer = new byte[WorldSizeBytes];
        using (var ms = new MemoryStream(worldBuffer))
        {
            World.Write(ms);
            SizeUsed = (int)ms.Position;
            ms.ToArray().CopyTo(worldBuffer, 0);
        }

        stream.Seek(sizeUsedPos, SeekOrigin.Begin);
        stream.WriteInt32(SizeUsed);
        stream.WriteBytes(worldBuffer);
        stream.Skip(SlotPaddingBytes);
    }
    
    public class GameSaveInfo
    {
        private const int PaddingBytes = 119;
        
        public int MissionsCompletedCount;
        public int ActivitiesCompletedCount;
        public int DistrictsLiberated;
        public int TerritoryIndex; //1 = terr01, 2 = dlc01
        public int DistrictIndex;
        public int Day;
        public int Month;
        public int Year;
        public int Hours;
        public int Minutes;
        public int Seconds;
        public bool IsAutoSave;
        public bool IsUsedSlot;
        public int DlcId; //-1 = base, 1 = dlc
        public uint SaveSlotIndex;
        public byte HammersUnlocked;
        public bool GoldenHammerDesired;
        public byte[] HammersUnlockedLarge = [];
        public byte[] BackpacksUnlocked = [];
        public byte JetpackUnlockLevel;

        public void Read(Stream stream)
        {
            MissionsCompletedCount = stream.ReadInt32();
            ActivitiesCompletedCount = stream.ReadInt32();
            DistrictsLiberated = stream.ReadInt32();
            TerritoryIndex = stream.ReadInt32();
            DistrictIndex = stream.ReadInt32();
            Day = stream.ReadInt32();
            Month = stream.ReadInt32();
            Year = stream.ReadInt32();
            Hours = stream.ReadInt32();
            Minutes = stream.ReadInt32();
            Seconds = stream.ReadInt32();
            IsAutoSave = stream.ReadBoolean8();
            IsUsedSlot = stream.ReadBoolean8();
            stream.Align(4);
            DlcId = stream.ReadInt32();
            SaveSlotIndex = stream.ReadUInt32();
            HammersUnlocked = stream.ReadUInt8();
            GoldenHammerDesired = stream.ReadBoolean8();
            HammersUnlockedLarge = stream.ReadBytes(8);
            BackpacksUnlocked = stream.ReadBytes(2);
            JetpackUnlockLevel = stream.ReadUInt8();
            stream.Skip(PaddingBytes);
        }

        public void Write(Stream stream)
        {
            stream.WriteInt32(MissionsCompletedCount);
            stream.WriteInt32(ActivitiesCompletedCount);
            stream.WriteInt32(DistrictsLiberated);
            stream.WriteInt32(TerritoryIndex);
            stream.WriteInt32(DistrictIndex);
            stream.WriteInt32(Day);
            stream.WriteInt32(Month);
            stream.WriteInt32(Year);
            stream.WriteInt32(Hours);
            stream.WriteInt32(Minutes);
            stream.WriteInt32(Seconds);
            stream.WriteBoolean(IsAutoSave);
            stream.WriteBoolean(IsUsedSlot);
            stream.Align(4);
            stream.WriteInt32(DlcId);
            stream.WriteUInt32(SaveSlotIndex);
            stream.WriteUInt8(HammersUnlocked);
            stream.WriteBoolean(GoldenHammerDesired);
            stream.WriteBytes(HammersUnlockedLarge);
            stream.WriteBytes(BackpacksUnlocked);
            stream.WriteUInt8(JetpackUnlockLevel);
            stream.Skip(PaddingBytes);
        }
    }
}