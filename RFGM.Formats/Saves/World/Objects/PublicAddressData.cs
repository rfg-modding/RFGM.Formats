using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Objects;

public class PublicAddressData
{
    public bool PublicAddressSpeakerListIsSorted;
    public List<PublicAddressSpeaker> PublicAddressSpeakers = new();
    public List<uint> KioskObjects = new();
    public List<KioskLine> KioskLines = new();
    public List<uint> KioskAvailableLines = new();

    public class PublicAddressSpeaker
    {
        public uint Speaker;
        public uint GroupNameCrc;
    }

    public class KioskLine
    {
        public uint Line;
        public int PlayCount;
    }

    public void Read(Stream stream)
    {
        var speakerCount = stream.ReadInt32();
        PublicAddressSpeakerListIsSorted = stream.ReadBoolean();
        for (var i = 0; i < speakerCount; i++)
        {
            PublicAddressSpeakers.Add(new PublicAddressSpeaker
            {
                Speaker = stream.ReadUInt32(),
                GroupNameCrc = stream.ReadUInt32()
            });
        }

        var kioskObjectCount = stream.ReadInt32();
        for (var i = 0; i < kioskObjectCount; i++)
        {
            KioskObjects.Add(stream.ReadUInt32());
        }

        var kioskLineCount = stream.ReadInt32();
        for (var i = 0; i < kioskLineCount; i++)
        {
            KioskLines.Add(new KioskLine
            {
                Line = stream.ReadUInt32(),
                PlayCount = stream.ReadInt32()
            });
        }

        var availableLineCount = stream.ReadInt32();
        for (var i = 0; i < availableLineCount; i++)
        {
            KioskAvailableLines.Add(stream.ReadUInt32());
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteInt32(PublicAddressSpeakers.Count);
        stream.WriteBoolean(PublicAddressSpeakerListIsSorted);
        foreach (var t in PublicAddressSpeakers)
        {
            stream.WriteUInt32(t.Speaker);
            stream.WriteUInt32(t.GroupNameCrc);
        }
            
        stream.WriteInt32(KioskObjects.Count);
        foreach (var t in KioskObjects)
        {
            stream.WriteUInt32(t);
        }

        stream.WriteInt32(KioskLines.Count);
        foreach (var t in KioskLines)
        {
            stream.WriteUInt32(t.Line);
            stream.WriteInt32(t.PlayCount);
        }

        stream.WriteInt32(KioskAvailableLines.Count);
        foreach (var t in KioskAvailableLines)
        {
            stream.WriteUInt32(t);
        }
    }
}