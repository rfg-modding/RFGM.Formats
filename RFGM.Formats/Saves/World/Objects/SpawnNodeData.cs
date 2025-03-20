using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Objects;

public class SpawnNodeData
{
    public string HostName = string.Empty;
    public bool IsValid;

    public void Read(Stream stream)
    {
        HostName = stream.ReadLengthPrefixedString16();
        IsValid = stream.ReadBoolean();
    }

    public void Write(Stream stream)
    {
        stream.WriteLengthPrefixedString16(HostName);
        stream.WriteBoolean(IsValid);
    }
}