using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Models;

public record Metadata(string Name, string Path, string Format, bool IsContainer, string Hash, long Length, Properties Properties)
{
    public string ToCsv() => $"{Name},{Path},{Format},{IsContainer},{Hash},{Length},{Properties.ToCsv()}";
    public static string ToCsvHeader() => $"{nameof(Name)},{nameof(Path)},{nameof(Format)},{nameof(IsContainer)},{nameof(Hash)},{nameof(Length)},{Properties.ToCsvHeader()}";
}
