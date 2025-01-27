using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record BuildMetadataMessage(EntryInfo EntryInfo, Breadcrumbs Breadcrumbs, Stream Primary) : IMessage;
