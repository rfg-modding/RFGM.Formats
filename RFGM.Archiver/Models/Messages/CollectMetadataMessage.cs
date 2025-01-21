using System.IO.Abstractions;

namespace RFGM.Archiver.Models;

public record CollectMetadataMessage(Stream Source, Stream? Secondary, string Name, Breadcrumbs Breadcrumbs, bool Hash) : IMessage;
