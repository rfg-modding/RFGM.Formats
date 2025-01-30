namespace RFGM.Formats.Localization;

public record Locatext(LocalizationHeader Header, IReadOnlyList<LocalizationEntry> Entries, string Name);