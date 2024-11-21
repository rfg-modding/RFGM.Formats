using System.Text;
using System.Text.Json.Serialization;

namespace RFGM.Formats.Localization
{
    public class LocalizationEntry
    {
        [JsonInclude]
        public string Identifier { get; internal set; } = string.Empty;

        [JsonInclude]
        public uint Hash { get; internal set; }

        [JsonIgnore]
        public uint Offset { get; internal set; }

        [JsonIgnore]
        public uint Length { get; internal set; }

        [JsonInclude]
        public string String { get; internal set; } = string.Empty;

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