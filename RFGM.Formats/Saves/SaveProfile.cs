using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves;

public class SaveProfile
{
    private const int PaddingBytes = 894 + 1024;
    
    public int MultiXp;
    public int MultiBonusXp;
    public uint MultiPlaylistId;
    public uint MultiFFAPlayerCRC;
    public uint MultiEDFPlayerCRC;
    public uint MultiGURPlayerCRC;
    public uint MultiBadge;
    public uint[] PlaylistHistory = new uint[10];
    public uint[] VetoHistory = new uint[10];
    public int SledgeUniqueId;
    public byte[] TooltipInfo = new byte[128];
    public bool GameCompleted;
    public bool WreckingCrewMapsUnlocked;
    public bool GoldenHammerUnlocked;
    public ushort StatsBufferSize;
    public byte[] StatsBuffer = [];
    public bool ImportedSaveData;
    public bool[,] CheatsUnlocked = new bool[2, 64]; //0 = Base 1 = DLC
    public bool GameCompletedDlc;

    public void Read(Stream stream)
    {
        MultiXp = stream.ReadInt32();
        MultiBonusXp = stream.ReadInt32();
        MultiPlaylistId = stream.ReadUInt32();
        MultiFFAPlayerCRC = stream.ReadUInt32();
        MultiEDFPlayerCRC = stream.ReadUInt32();
        MultiGURPlayerCRC = stream.ReadUInt32();
        MultiBadge = stream.ReadUInt32();

        for (var i = 0; i < PlaylistHistory.Length; i++)
        {
            PlaylistHistory[i] = stream.ReadUInt32();
        }

        for (var i = 0; i < VetoHistory.Length; i++)
        {
            VetoHistory[i] = stream.ReadUInt32();
        }

        SledgeUniqueId = stream.ReadInt32();
        TooltipInfo = stream.ReadBytes(TooltipInfo.Length);
        GameCompleted = stream.ReadBoolean();
        WreckingCrewMapsUnlocked = stream.ReadBoolean();
        GoldenHammerUnlocked = stream.ReadBoolean();
        StatsBufferSize = stream.ReadUInt16();
        StatsBuffer = stream.ReadBytes(2250);
        ImportedSaveData = stream.ReadBoolean();

        for (var i = 0; i < CheatsUnlocked.GetLength(0); i++)
        {
            for (var j = 0; j < CheatsUnlocked.GetLength(1); j++)
            {
                CheatsUnlocked[i, j] = stream.ReadBoolean8();
            }
        }

        GameCompletedDlc = stream.ReadBoolean();
        stream.Skip(PaddingBytes);
    }

    public void Write(Stream stream)
    {
        stream.WriteInt32(MultiXp);
        stream.WriteInt32(MultiBonusXp);
        stream.WriteUInt32(MultiPlaylistId);
        stream.WriteUInt32(MultiFFAPlayerCRC);
        stream.WriteUInt32(MultiEDFPlayerCRC);
        stream.WriteUInt32(MultiGURPlayerCRC);
        stream.WriteUInt32(MultiBadge);

        foreach (var t in PlaylistHistory)
        {
            stream.WriteUInt32(t);
        }

        foreach (var t in VetoHistory)
        {
            stream.WriteUInt32(t);
        }

        stream.WriteInt32(SledgeUniqueId);
        stream.WriteBytes(TooltipInfo);
        stream.WriteBoolean(GameCompleted);
        stream.WriteBoolean(WreckingCrewMapsUnlocked);
        stream.WriteBoolean(GoldenHammerUnlocked);
        stream.WriteUInt16(StatsBufferSize);
        stream.WriteBytes(StatsBuffer);
        stream.WriteBoolean(ImportedSaveData);

        for (var i = 0; i < CheatsUnlocked.GetLength(0); i++)
        {
            for (var j = 0; j < CheatsUnlocked.GetLength(1); j++)
            {
                stream.WriteBoolean8(CheatsUnlocked[i, j]);
            }
        }

        stream.WriteBoolean(GameCompletedDlc);
        stream.Skip(PaddingBytes);
    }
}