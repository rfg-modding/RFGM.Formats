using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;

namespace RFGM.Archiver.Services;

public class Worker(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> log)
{
    public int Pending => actionBlock.InputCount;
    public int InFlight => inFlight;
    public IReadOnlyList<Failure> Failed => failed;

    private ActionBlock<IMessage> actionBlock = null!;
    private CancellationToken cancellationToken;
    private int inFlight = 0;
    private readonly List<Failure> failed = new();
    private const int Concurrency = 4; // TODO read from args
    private static readonly object Locker = new();


    public async Task Start(IMessage initialValues, CancellationToken token) => await Start([initialValues], token);

    public async Task Start(IEnumerable<IMessage> initialValues, CancellationToken token)
    {
        actionBlock = new ActionBlock<IMessage>(HandleMessage, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Concurrency,
            CancellationToken = token,
        });
        cancellationToken = token;
        foreach (var initialValue in initialValues)
        {
            actionBlock.Post(initialValue);
        }
        await actionBlock.Completion;
    }

    private async Task HandleMessage(IMessage message)
    {
        IEnumerable<IMessage>? newMessages = null;
        try
        {
            lock (Locker)
            {
                Interlocked.Increment(ref inFlight);
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            newMessages = await HandleInternal(scope.ServiceProvider, message, cancellationToken);
        }
        catch (Exception e)
        {
            var failure = new Failure(message, "HandleMessage exception", e);
            failed.Add(failure);
            log.LogError("Fail! {failure}", failure);
        }
        finally
        {
            lock (Locker)
            {
                if (newMessages != null)
                {
                    foreach (var newMessage in newMessages)
                    {
                        var result = actionBlock.Post(newMessage);
                        if (!result)
                        {
                            var failure = new Failure(message, $"ActionBlock Post failed, canceled={cancellationToken.IsCancellationRequested}", null);
                            failed.Add(failure);
                            log.LogError("Fail! {failure}", failure);
                        }
                    }

                }

                var actualInFlight = Interlocked.Decrement(ref inFlight);
                if (actualInFlight == 0 && actionBlock.InputCount == 0)
                {
                    actionBlock.Complete();
                    log.LogInformation("Finished: no pending tasks");
                }
            }
        }
    }

    private async Task<IEnumerable<IMessage>> HandleInternal(IServiceProvider services, IMessage message, CancellationToken token)
    {
        var handlerType = typeof(IHandler<>).MakeGenericType(message.GetType());
        var service = services.GetService(handlerType);
        if (service is null)
        {
            throw new ArgumentException($"Handler {handlerType.FullName} not found");
        }

        var handler = service as IHandler<IMessage>;
        if (handler is null)
        {
            throw new ArgumentException($"Failed to cast handler {handlerType.FullName} to base interface");
        }

        return await handler.Handle(message, token);
    }
}
