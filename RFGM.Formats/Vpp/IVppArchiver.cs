using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Vpp;

public interface IVppArchiver
{
    Task<LogicalArchive> UnpackVpp(Stream source, string name, CancellationToken token);

    Task PackVpp(LogicalArchive logicalArchive, Stream destination, CancellationToken token);
}
