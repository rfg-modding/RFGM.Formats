namespace RFGM.Formats.Abstractions;

public record EntryInfo(
    string Name,
    IFormatDescriptor Descriptor,
    Properties Properties
)
{
    public string FileName => Descriptor.GetFileName(this);
    public string DirectoryName => Descriptor.GetDirectoryName(this);

    public override string ToString() => $"{Name}@{Descriptor.Format}, props={Properties}";
}
