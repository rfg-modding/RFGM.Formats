using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Territory;

public class CriticalAreaData
{
    public uint Flags;
    public uint Handle;
    public float Radius;
    public uint TmrHandle;

    public void Read(Stream stream)
    {
        Handle = stream.ReadUInt32();
        Radius = stream.ReadFloat();
        Flags = stream.ReadUInt32();
        TmrHandle = stream.ReadUInt32();
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32(Handle);
        stream.WriteFloat(Radius);
        stream.WriteUInt32(Flags);
        stream.WriteUInt32(TmrHandle);
    }
}