using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Messages;

namespace RFGM.Archiver.Services.Handlers;

public class LocateHandler(ArchiverState archiverState, AsmFileLocator asmFileLocator, ILogger<LocateHandler> log) : HandlerBase<LocateMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(LocateMessage message, CancellationToken token)
    {
        var location = await asmFileLocator.LocateFileForPrimitive(message, token);
        archiverState.PrimitiveLocations.Add(location);
        return [];
    }
}
