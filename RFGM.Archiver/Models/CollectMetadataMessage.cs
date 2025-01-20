using System.IO.Abstractions;

namespace RFGM.Archiver.Models;

public record CollectMetadataMessage(Stream Source, Stream? Secondary, string Name, IReadOnlyList<string> RelativePath, bool Hash) : IMessage;
