using System.Text;
using System.Xml.Serialization;

namespace RFGM.Formats.Localization
{
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

        public LocalizationEntry() { }

        public LocalizationEntry(BinaryReader reader)
        {
            Hash = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
            Length = reader.ReadUInt32();
        }

        public void LoadString(BinaryReader reader)
        {
            reader.BaseStream.Seek(Offset, SeekOrigin.Begin);
            byte[] stringBytes = reader.ReadBytes((int)Length);
            String = Encoding.Unicode.GetString(stringBytes).TrimEnd('\0');
        }
    }
}