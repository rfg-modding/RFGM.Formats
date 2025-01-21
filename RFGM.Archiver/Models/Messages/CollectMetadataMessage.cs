using RFGM.Archiver.Models.Metadata;
using RFGM.Formats;

namespace RFGM.Archiver.Models.Messages;

public record CollectMetadataMessage(Stream Source, Stream? Secondary, string Name, Breadcrumbs Breadcrumbs, bool Hash, OptimizeFor OptimizeFor) : IMessage
{
    public override string ToString() => $"{Name} {Breadcrumbs} hash={Hash}\n\tsource=    {Source}\n\n\tsecondary= {Secondary}";
}
