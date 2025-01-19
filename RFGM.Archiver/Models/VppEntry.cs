namespace RFGM.Archiver.Models;

public record VppEntry(string Name, string RelativePath, int Order, uint Offset, long Length, uint CompressedSize, string Hash) : IMetadata
{
    public override string ToString() => $"{RelativePath} {nameof(Order)}={Order} {nameof(Offset)}={Offset} {nameof(Length)}={Length} " +
                                         $"{nameof(CompressedSize)}={CompressedSize} {nameof(Hash)}={Hash}";
}