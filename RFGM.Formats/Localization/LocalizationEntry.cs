using System.Text;
using System.Xml.Serialization;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Localization;

public class LocalizationEntry
{
    [XmlElement("Identifier")]
    public string Identifier { get; set; } = string.Empty;

    [XmlElement("Hash")]
    public uint Hash { get; set; }

    [XmlIgnore]
    public uint Offset { get; set; }

    [XmlIgnore]
    public uint Length { get; set; }

    [XmlElement("String")]
    public string String { get; set; } = string.Empty;

    public void Read(Stream stream)
    {
        Hash = stream.ReadUInt32();
        Offset = stream.ReadUInt32();
        Length = stream.ReadUInt32();
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32(Hash);
        stream.WriteUInt32(Offset);
        stream.WriteUInt32(Length);
    }

    public void ReadString(Stream stream)
    {
        stream.Seek(Offset, SeekOrigin.Begin);
        var stringBytes = stream.ReadBytes((int)Length);
        String = Encoding.Unicode.GetString(stringBytes).TrimEnd('\0');
    }

    public void WriteString(Stream stream)
    {
        var textBytes = Encoding.Unicode.GetBytes(String + "\0");
        stream.Write(textBytes);
    }
}