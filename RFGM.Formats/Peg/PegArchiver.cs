using Microsoft.Extensions.Logging;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Vpp;

namespace RFGM.Formats.Peg;

public class PegArchiver(ILogger<PegArchiver> log)
    : IPegArchiver
{
    public async Task<LogicalTextureArchive> UnpackPeg(PegStreams streams, string name, CancellationToken token)
    {
        log.LogTrace("Unpacking peg [{name}]", name);
        var reader = new PegReader();
        return await Task.Run(() =>
        {
            var peg = reader.Read(streams.Cpu, streams.Gpu, name, token);
            return peg;
        }, token);
    }

    public async Task PackPeg(LogicalTextureArchive logicalTextureArchive, PegStreams streams, CancellationToken token)
    {
        log.LogTrace("Packing peg [{name}]", logicalTextureArchive.Name);
        using var writer = new PegWriter(logicalTextureArchive);
        await writer.WriteAll(streams.Cpu, streams.Gpu, token);
    }
}
