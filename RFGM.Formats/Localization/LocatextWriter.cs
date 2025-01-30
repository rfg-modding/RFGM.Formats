using System.Text;
using System.Xml;
using RFGM.Formats.Hashes;

namespace RFGM.Formats.Localization;

public class LocatextWriter
{
    public void Write(Locatext locatext, Stream s)
    {
        locatext.Header.Write(s);
        var offset = (uint)(12 + locatext.Entries.Count * 12);
        foreach (var entry in locatext.Entries)
        {
            entry.Hash = Hash.HashVolitionCRCAlt(entry.Identifier);
            entry.Offset = offset;
            entry.Length = (uint)Encoding.Unicode.GetByteCount(entry.String + "\0");
            offset += entry.Length;
        }

        foreach (var entry in locatext.Entries)
        {
            entry.Write(s);
        }

        foreach (var entry in locatext.Entries)
        {
            entry.WriteString(s);
        }
    }

    public void WriteXml(Locatext locatext, Stream s)
    {
        using var writer = XmlWriter.Create(s, Settings);
        writer.WriteStartElement("root");
        foreach (var entry in locatext.Entries)
        {
            writer.WriteStartElement("Entry");
            writer.WriteElementString("Identifier", entry.Identifier);
            writer.WriteElementString("Hash", entry.Hash.ToString());
            writer.WriteElementString("String", entry.String);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static readonly XmlWriterSettings Settings = new()
    {
        Indent = true,
        Encoding = Encoding.UTF8,
        OmitXmlDeclaration = true
    };
}