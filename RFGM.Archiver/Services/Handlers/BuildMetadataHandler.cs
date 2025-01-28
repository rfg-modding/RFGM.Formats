using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;

namespace RFGM.Archiver.Services.Handlers;

public class BuildMetadataHandler() : HandlerBase<BuildMetadataMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(BuildMetadataMessage message, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var name = message.EntryInfo.Name;
        var path = message.Breadcrumbs.Descend(name).ToString();
        var format = message.EntryInfo.Descriptor.Name;
        var isContainer = message.EntryInfo.Descriptor.IsContainer;
        var hash = await FormatUtils.ComputeHash(message.Primary, token);
        var length = message.Primary.Length;

        //NOTE: entryInfo.Properties are updated from other tasks when descending into nested archives, etc.
        //Store them until last moment, serialize only when all work is done
        //But calculate hashes and release streams ASAP!
        Archiver.Metadata.Add(new Metadata(name, path, format, isContainer, hash, length, message.EntryInfo.Properties));
        return [];
    }
}
