using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Archiver.Services.Handlers;

namespace RFGM.Archiver.Services;

public class Worker(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> log)
{
    public int Pending => actionBlock.InputCount;
    public int InFlight => inFlight;
    public IReadOnlyList<Failure> Failed => failed;

    private ActionBlock<IMessage> actionBlock = null!;
    private CancellationToken cancellationToken;
    private int inFlight;
    private int posted;
    private int finished;
    private readonly List<Failure> failed = new();
    private readonly Queue<IMessage> lowPriority = new();
    private static readonly object Locker = new();


    public async Task Start(IMessage initialValue, int parallel, CancellationToken token) => await Start([initialValue], parallel, token);

    public async Task Start(IEnumerable<IMessage> initialValues, int parallel, CancellationToken token)
    {
        actionBlock = new ActionBlock<IMessage>(HandleMessage, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = parallel,
            CancellationToken = token,
        });
        cancellationToken = token;
        foreach (var x in initialValues)
        {
            lowPriority.Enqueue(x);
        }

        if (!lowPriority.Any())
        {
            log.LogWarning("Nothing to process");
            return;
        }

        var m = lowPriority.Dequeue();
        log.LogTrace("Posting initial message {message}", m);
        actionBlock.Post(m);
        Interlocked.Increment(ref posted);
        await actionBlock.Completion;

    }

    private async Task HandleMessage(IMessage message)
    {
        List<IMessage> newMessages = [];
        try
        {
            lock (Locker)
            {
                Interlocked.Increment(ref inFlight);
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            log.LogTrace("Run [{message}]", message);
            var result = await HandleInternal(scope.ServiceProvider, message, cancellationToken);
            newMessages = result.ToList();
            Interlocked.Increment(ref finished);
            log.LogTrace("End [{message}], result=[{count}]", message, newMessages.Count);
        }
        catch (OperationCanceledException e)
        {
            var failure = new Failure(message, "HandleMessage canceled", e);
            failed.Add(failure);
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
                HandleFinally(message, newMessages);
            }
        }
    }

    private void HandleFinally(IMessage message, List<IMessage> newMessages)
    {
        var actualInFlight = Interlocked.Decrement(ref inFlight);
        if (cancellationToken.IsCancellationRequested)
        {
            var failure = new Failure(message, $"ActionBlock canceled", null);
            failed.Add(failure);
            actionBlock.Complete();
            log.LogError("Canceled. Tasks still running: {tasks}", actualInFlight);
            return;
        }

        foreach (var newMessage in newMessages)
        {
            var result = actionBlock.Post(newMessage);
            Interlocked.Increment(ref posted);
            if (!result)
            {
                var failure = new Failure(message, $"ActionBlock Post failed, canceled={cancellationToken.IsCancellationRequested}", null);
                failed.Add(failure);
                log.LogError("Fail! {failure}", failure);
            }
        }


        if (actualInFlight == 0 && actionBlock.InputCount == 0 && newMessages.Count == 0)
        {
            if (lowPriority.Any())
            {
                // when nothing else to do, start next initial task
                var x = lowPriority.Dequeue();
                log.LogTrace("Posting initial message {message}", x);
                actionBlock.Post(x);
                Interlocked.Increment(ref posted);
            }
            else
            {
                actionBlock.Complete();
                log.LogDebug("Finished {finished}/{posted} tasks", finished, posted);
            }
        }
    }

    private async Task<IEnumerable<IMessage>> HandleInternal(IServiceProvider services, IMessage message, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
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
