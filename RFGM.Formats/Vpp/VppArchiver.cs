using Microsoft.Extensions.Logging;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Vpp;

public class VppArchiver(ILogger<VppArchiver> log)
    : IVppArchiver
{
    public async Task<LogicalArchive> UnpackVpp(Stream source, string name, CancellationToken token)
    {
        log.LogTrace("Unpacking vpp [{name}]", name);
        var reader = new VppReader(OptimizeFor.speed);
        return await Task.Run(() => reader.Read(source, name, token), token);
    }

    public async Task PackVpp(LogicalArchive logicalArchive, Stream destination, CancellationToken token)
    {
        log.LogTrace("Packing vpp [{name}]", logicalArchive.Name);
        using var writer = new VppWriter(logicalArchive);
        await writer.WriteAll(destination, token);
    }
}
