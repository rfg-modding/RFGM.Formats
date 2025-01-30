namespace RFGM.Formats.Abstractions;

public record EntryInfo(
    string Name,
    IFormatDescriptor Descriptor,
    Properties Properties
)
{
    public override string ToString() => $"{Name}@{Descriptor.Name}, props={Properties}";

    public EntryInfo MakeDeepCopy() => this with {Properties = Properties with { }};
}
