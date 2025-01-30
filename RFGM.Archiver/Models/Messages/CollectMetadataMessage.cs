using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record CollectMetadataMessage(EntryInfo EntryInfo, Breadcrumbs Breadcrumbs, Stream Primary) : IMessage;
