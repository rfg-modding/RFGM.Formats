using RFGM.Formats.Streams;

namespace RFGM.Formats.Localization;

public class LocalizationHeader
{
    public const uint ExpectedSignature = 2823585651;
    public const uint ExpectedVersion = 3;

    public uint Signature { get; set; }
    public uint Version { get; set; }
    public uint StringCount { get; set; }

    public void Read(Stream stream)
    {
        Signature = stream.ReadUInt32();
        Version = stream.ReadUInt32();
        StringCount = stream.ReadUInt32();

        if (Signature != ExpectedSignature)
        {
            throw new FormatException($"Invalid file signature. Expected {ExpectedSignature}, but found {Signature}");
        }

        if (Version != ExpectedVersion)
        {
            throw new FormatException($"Invalid file version. Expected {ExpectedVersion}, but found {Version}");
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32(ExpectedSignature);
        stream.WriteUInt32(ExpectedVersion);
        stream.WriteUInt32(StringCount);
    }
}
