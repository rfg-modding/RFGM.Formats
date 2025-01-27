using RFGM.Archiver.Models.Messages;

namespace RFGM.Archiver.Services.Handlers;

public abstract class HandlerBase<TMessage> : IHandler<TMessage> where TMessage : class, IMessage
{
    public async Task<IEnumerable<IMessage>> Handle(IMessage message, CancellationToken token)
    {
        var cast = message as TMessage;
        if (cast is null)
        {
            throw new ArgumentOutOfRangeException(nameof(message), message.GetType().Name, $"Expected message type {typeof(TMessage).Name}");
        }

        return await Handle(cast, token);
    }

    public abstract Task<IEnumerable<IMessage>> Handle(TMessage message, CancellationToken token);
}