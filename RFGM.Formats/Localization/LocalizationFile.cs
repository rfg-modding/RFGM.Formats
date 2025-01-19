using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RFGM.Formats.Localization
{
    public class LocalizationFile
    {
        private const uint ExpectedSignature = 2823585651;
        private const uint ExpectedVersion = 3;

        // Header
        public uint Signature { get; private set; }
        public uint Version { get; private set; }
        public uint StringCount { get; private set; }

        public List<LocalizationEntry> Entries { get; private set; } = new List<LocalizationEntry>();

        public void Read(BinaryReader reader, string xtblPath)
        {
            ValidateHeader(reader);

            if (!string.IsNullOrEmpty(xtblPath))
            {
                LocalizationScraper.GetIdentifiers(xtblPath);
            }

            for (int i = 0; i < StringCount; i++)
            {
                var entry = new LocalizationEntry(reader);
                if (LocalizationScraper.StringIdentifiers.TryGetValue(entry.Hash, out string? identifier))
                {
                    entry.Identifier = identifier;
                }
                Entries.Add(entry);
            }

            foreach (var entry in Entries)
            {
                entry.LoadString(reader);
            }
        }

        public static void Write(string outputPath, List<LocalizationEntry> entries)
        {
            using var reader = new BinaryWriter(File.Open(outputPath, FileMode.Create, FileAccess.Write));
            var localizationFile = new LocalizationFile
            {
                Signature = ExpectedSignature,
                Version = ExpectedVersion,
                StringCount = (uint)entries.Count,
                Entries = entries
            };

            reader.Write(localizationFile.Signature);
            reader.Write(localizationFile.Version);
            reader.Write(localizationFile.StringCount);

            uint offset = (uint)(12 + entries.Count * 12);
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Identifier))
                {
                    uint computedHash = LocalizationScraper.HashVolitionCRCAlt(entry.Identifier);
                    if (entry.Hash != computedHash)
                    {
                        entry.Hash = computedHash;
                    }
                }

                entry.Offset = offset;
                entry.Length = (uint)Encoding.Unicode.GetByteCount(entry.String + "\0");
                offset += entry.Length;
            }

            foreach (var entry in entries)
            {
                reader.Write(entry.Hash);
                reader.Write(entry.Offset);
                reader.Write(entry.Length);
            }

            foreach (var entry in entries)
            {
                var textBytes = Encoding.Unicode.GetBytes(entry.String + "\0");
                reader.Write(textBytes);
            }
        }

        private void ValidateHeader(BinaryReader reader)
        {
            Signature = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            StringCount = reader.ReadUInt32();

            if (Signature != ExpectedSignature)
            {
                throw new FormatException($"Invalid file signature. Expected {ExpectedSignature}, but found {Signature}");
            }

            if (Version != ExpectedVersion)
            {
                throw new FormatException($"Invalid file version. Expected {ExpectedVersion}, but found {Version}");
            }
        }

        public void ConvertToXml(string outputPath)
        {
            if (Entries == null || Entries.Count == 0)
            {
                throw new InvalidOperationException("Entries cannot be null or empty.");
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = true
            };

            using var writer = XmlWriter.Create(outputPath, settings);

            writer.WriteStartElement("root");

            foreach (var entry in Entries)
            {
                writer.WriteStartElement("Entry");
                writer.WriteElementString("Identifier", entry.Identifier);
                writer.WriteElementString("Hash", entry.Hash.ToString());
                writer.WriteElementString("String", entry.String);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public static void ConvertFromXml(string xmlPath, string outputPath)
        {
            if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
            {
                throw new ArgumentException("File path is invalid or the file does not exist.", nameof(xmlPath));
            }

            var entries = new List<LocalizationEntry>();
            var document = XDocument.Load(xmlPath);

            foreach (var entryElement in document.Descendants("Entry"))
            {
                var entry = new LocalizationEntry
                {
                    Identifier = entryElement.Element("Identifier")?.Value ?? string.Empty,
                    Hash = uint.TryParse(entryElement.Element("Hash")?.Value, out var hash) ? hash : 0,
                    String = entryElement.Element("String")?.Value ?? string.Empty
                };

                entries.Add(entry);
            }

            Write(outputPath, entries);
        }
    }
}