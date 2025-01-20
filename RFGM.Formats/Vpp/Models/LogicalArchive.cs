namespace RFGM.Formats.Vpp.Models;

public record LogicalArchive(IEnumerable<LogicalFile> LogicalFiles, RfgVpp.HeaderBlock.Mode Mode, string Name)
{
    public string UnpackName => $"{Name}.{Mode.ToString().ToLowerInvariant()}";
}
