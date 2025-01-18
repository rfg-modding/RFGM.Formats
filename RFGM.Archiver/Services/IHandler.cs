using RFGM.Archiver.Models;

namespace RFGM.Archiver.Services;

public interface IHandler<out TMessage> where TMessage : class, IMessage
{
    public Task<IEnumerable<IMessage>> Handle(IMessage message, CancellationToken token);
}