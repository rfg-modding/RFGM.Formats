using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;

namespace RFGM.Archiver.Services;

public class PackHandler(ILogger<PackHandler> log) : HandlerBase<PackMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(PackMessage message, CancellationToken token)
    {
        log.LogDebug("pack: {message}", message);
        return [];
    }
}
