using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Peg;

public interface IPegArchiver
{
    Task<LogicalTextureArchive> UnpackPeg(PairedStreams streams, string name, CancellationToken token);

    Task PackPeg(LogicalTextureArchive logicalTextureArchive, PairedStreams streams, CancellationToken token);
}
