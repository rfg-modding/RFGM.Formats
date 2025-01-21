namespace RFGM.Archiver.Models.Metadata;

public record VppEntry(string Name, string RelativePath, int Order, uint Offset, string Length, uint CompressedSize, string Hash) : IMetadata
{
    public override string ToString() => $"{RelativePath} {nameof(Order)}={Order} {nameof(Offset)}={Offset} {nameof(Length)}={Length} " +
                                         $"{nameof(CompressedSize)}={CompressedSize} {nameof(Hash)}={Hash}";
}