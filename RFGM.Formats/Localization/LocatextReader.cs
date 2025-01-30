using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using RFGM.Formats.Hashes;

namespace RFGM.Formats.Localization;

public class LocatextReader(ILogger<LocatextReader> log)
{
    public Locatext Read(Stream s, string name, IReadOnlyList<string> xtblFiles)
    {
        var allIdentifiers = GetIdentifiers(xtblFiles);
        var header = new LocalizationHeader();
        header.Read(s);
        var entries = Enumerable.Range(0, (int) header.StringCount)
            .Select(_ => ReadEntry(s, allIdentifiers))
            .ToList();
        foreach (var entry in entries)
        {
            entry.ReadString(s);
        }

        return new Locatext(header, entries, name);
    }

    public Locatext ReadXml(Stream s, string name)
    {
        var document = XDocument.Load(s);
        var entries = document
            .Descendants("Entry")
            .Select(ParseEntry)
            .ToList();
        var header = new LocalizationHeader
        {
            Signature = LocalizationHeader.ExpectedSignature,
            Version = LocalizationHeader.ExpectedVersion,
            StringCount = (uint) entries.Count
        };
        return new Locatext(header, entries, name);
    }

    private LocalizationEntry ReadEntry(Stream s, IReadOnlyDictionary<uint, string> identifiers)
    {
        LocalizationEntry entry = new();
        entry.Read(s);
        if (identifiers.TryGetValue(entry.Hash, out var identifier))
        {
            entry.Identifier = identifier;
        }

        return entry;
    }

    private IReadOnlyDictionary<uint, string> GetIdentifiers(IReadOnlyList<string> xtblFiles)
    {
        var d = new Dictionary<uint, string>(KnownStrings.ByHash);
        var allIdentifiers = xtblFiles.SelectMany(ScrapeXtblIdentifiers);
        foreach (var x in allIdentifiers)
        {
            if (d.ContainsKey(x.Key))
            {
                var existingValue = d[x.Key];
                if (existingValue.Equals(x.Value, StringComparison.OrdinalIgnoreCase))
                {
                    log.LogWarning("Hash collision for [{key}], in-game text may be displayed incorrectly! Existing string [{existing}], new string [{new}]", x.Key, existingValue, x.Value);
                }
                continue;
            }
            d.Add(x.Key, x.Value);
        }

        return d;
    }

    private IEnumerable <KeyValuePair <uint, string>> ScrapeXtblIdentifiers(string xtbl) =>
        XDocument.Parse(xtbl).Descendants().Where(e => !e.HasElements)
            .Select(x => new KeyValuePair<uint, string>(Hash.HashVolitionCRCAlt(x.Value), x.Value));

    private LocalizationEntry ParseEntry(XElement element) =>
        new()
        {
            Identifier = element.Element("Identifier")?.Value ?? string.Empty,
            Hash = uint.TryParse(element.Element("Hash")?.Value, out var hash) ? hash : 0,
            String = element.Element("String")?.Value ?? string.Empty
        };
}