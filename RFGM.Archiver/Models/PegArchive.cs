namespace RFGM.Archiver.Models;

public record PegArchive(string Name, string RelativePath, string Length, int Align, string Hash, int Entries) : IMetadata
{
    public override string ToString() => $"{RelativePath} {nameof(Length)}={Length} {nameof(Align)}={Align} " +
                                         $"{nameof(Entries)}={Entries} {nameof(Hash)}={Hash}";
}