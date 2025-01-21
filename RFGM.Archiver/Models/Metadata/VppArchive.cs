using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Models.Metadata;

public record VppArchive(string Name, string RelativePath, RfgVpp.HeaderBlock.Mode Mode, string Length, string Hash, int Entries) : IMetadata
{
    public override string ToString() => $"{RelativePath} {nameof(Mode)}={Mode} {nameof(Length)}={Length} " +
                                         $"{nameof(Entries)}={Entries} {nameof(Hash)}={Hash}";
}
