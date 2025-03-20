using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves;

public class SaveHeader
{
    private const int ExpectedVersion = 16777216;

    public uint Version;
    public uint Checksum;
    public string DeveloperText = "";

    public void Read(Stream stream)
    {
        Version = stream.ReadUInt32();
            
        if (Version != ExpectedVersion)
            throw new InvalidDataException($"Invalid save file version detected. Expected {ExpectedVersion}, read {Version}.");

        Checksum = stream.ReadUInt32();
        DeveloperText = stream.ReadAsciiString(256);
        stream.Skip(128);
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32(Version);
        stream.WriteUInt32(Checksum);
        stream.WriteAsciiString(DeveloperText);
        stream.Skip(128);
    }
}