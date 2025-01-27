using RFGM.Archiver.Models.Messages;

namespace RFGM.Archiver.Services.Handlers;

public interface IHandler<out TMessage> where TMessage : class, IMessage
{
    public Task<IEnumerable<IMessage>> Handle(IMessage message, CancellationToken token);
}