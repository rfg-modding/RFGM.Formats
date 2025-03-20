using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Metadata;

public class TimeData
{
    public byte Hour;
    public byte Minutes;
    public byte Seconds;

    public void Read(Stream stream)
    {
        Hour = stream.ReadUInt8();
        Minutes = stream.ReadUInt8();
        Seconds = stream.ReadUInt8();
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt8(Hour);
        stream.WriteUInt8(Minutes);
        stream.WriteUInt8(Seconds);
    }
}