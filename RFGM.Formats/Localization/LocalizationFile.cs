using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

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
            using var reader = new BinaryWriter(File.Open(outputPath, FileMode.Create));
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

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public void ConvertToJson(string inputPath)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(inputPath));
            }

            if (Entries == null)
            {
                throw new InvalidOperationException("Entries cannot be null");
            }

            string json = JsonSerializer.Serialize(Entries, JsonSerializerOptions);

            try
            {
                File.WriteAllText(inputPath, json);
            }
            catch (IOException ex)
            {
                throw new IOException($"Failed to write JSON to file: {inputPath}", ex);
            }
        }

        public static void ConvertFromJson(string jsonPath, string outputPath)
        {
            var json = File.ReadAllText(jsonPath);
            var localizationEntries = JsonSerializer.Deserialize<List<LocalizationEntry>>(json);

            if (localizationEntries == null || localizationEntries.Count == 0)
            {
                throw new InvalidOperationException("The JSON file is empty or could not be deserialized.");
            }

            Write(outputPath, localizationEntries);
        }
    }
}