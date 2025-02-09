using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Messages;

namespace RFGM.Archiver.Services.Handlers;

public class UpdateAsmHandler(ArchiverState archiverState, AsmUpdater asmUpdater, ILogger<UpdateAsmHandler> log) : HandlerBase<UpdateAsmMessage>
{
    public override async Task<IEnumerable<IMessage>> Handle(UpdateAsmMessage message, CancellationToken token)
    {
        var (vpp, rfgDataDir) = message;
        await asmUpdater.Update(vpp, rfgDataDir, token);
        //archiverState.??.Add(??);
        return [];
    }
}
