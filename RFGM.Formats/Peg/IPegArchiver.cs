using RFGM.Formats.Peg.Models;

namespace RFGM.Formats.Peg;

public interface IPegArchiver
{
    Task<LogicalTextureArchive> UnpackPeg(PegStreams streams, string name, CancellationToken token);

    Task PackPeg(LogicalTextureArchive logicalTextureArchive, PegStreams streams, CancellationToken token);
}
