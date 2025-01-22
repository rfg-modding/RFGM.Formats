using RFGM.Formats.Hashes;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RFGM.Formats.Localization;

public class LocalizationFile
{
    public LocalizationHeader Header { get; set; } = new();
    public List<LocalizationEntry> Entries { get; set; } = new();

    /// <summary>
    /// Reads an rfglocatext file.
    /// </summary>
    public void Read(Stream stream) => Read(stream, string.Empty);

    /// <summary>
    /// Reads an rfglocatext file. Identifiers will be scraped from the specified xtbl folder path.
    /// </summary>
    /// <param name="xtblPath">Path to a directory containing xtbl files.</param>
    public void Read(Stream stream, string xtblPath)
    {
        Header.Read(stream);

        if (!string.IsNullOrEmpty(xtblPath))
        {
            LocalizationScraper.GetIdentifiers(xtblPath);
        }

        for (int i = 0; i < Header.StringCount; i++)
        {
            LocalizationEntry entry = new();
            entry.Read(stream);

            if (LocalizationScraper.StringIdentifiers.TryGetValue(entry.Hash, out string? identifier))
            {
                entry.Identifier = identifier;
            }
            Entries.Add(entry);
        }

        foreach (var entry in Entries)
        {
            entry.ReadString(stream);
        }
    }

    /// <summary>
    /// Writes an rfglocatext file.
    /// </summary>
    public void Write(Stream stream)
    {
        Header.StringCount = (uint)Entries.Count;
        Header.Write(stream);

        uint offset = (uint)(12 + Entries.Count * 12);
        foreach (var entry in Entries)
        {
            if (!string.IsNullOrEmpty(entry.Identifier))
            {
                uint computedHash = Hash.HashVolitionCRCAlt(entry.Identifier);
                if (entry.Hash != computedHash)
                {
                    entry.Hash = computedHash;
                }
            }

            entry.Offset = offset;
            entry.Length = (uint)Encoding.Unicode.GetByteCount(entry.String + "\0");
            offset += entry.Length;
        }

        foreach (var entry in Entries)
        {
            entry.Write(stream);
        }

        foreach (var entry in Entries)
        {
            entry.WriteString(stream);
        }
    }

    /// <summary>
    /// Reads entries from an XML file and populates the Entries list.
    /// </summary>
    public void ReadFromXml(Stream stream)
    {
        var entries = new List<LocalizationEntry>();
        var document = XDocument.Load(stream);

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

        Entries = entries;
    }

    /// <summary>
    /// Writes entries to an XML file.
    /// </summary>
    public void WriteToXml(Stream stream)
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

        using var writer = XmlWriter.Create(stream, settings);

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
}