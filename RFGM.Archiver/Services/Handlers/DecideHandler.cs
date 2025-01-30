using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Abstractions.Descriptors;

namespace RFGM.Archiver.Services.Handlers;

/// <summary>
/// Decide what to do with arguments passed to .exe without explicit command
/// </summary>
public class DecideHandler(IFileSystem fileSystem, ILogger<DecideHandler> log) : HandlerBase<DecideMessage>
{
    public override Task<IEnumerable<IMessage>> Handle(DecideMessage message, CancellationToken token)
    {
        return Task.FromResult(HandleInternal(message));
    }

    private IEnumerable<IMessage> HandleInternal(DecideMessage message)
    {
        var pack = message.PackSettings is not null;
        var unpack = message.UnpackSettings is not null;
        var decision = Decide(message.Input, pack, unpack);
        switch (decision)
        {
            case ErrorDecision e:
                log.LogError("{message} [{input}]", e.Error, message.Input);
                return [];
            case PackDecision p when pack:
                log.LogInformation("{message} [{input}]", p.Message, message.Input);
                return [new StartPackMessage(p.FileSystemInfo, message.Output ?? Constants.DefaultPackDir, message.PackSettings!)];
            case UnpackDecision u when unpack:
                log.LogInformation("{message} [{input}]", u.Message, message.Input);
                return [new StartUnpackMessage(u.FileInfo, message.Output ?? Constants.DefaultUnpackDir, message.UnpackSettings!)];
            default:
                throw new ArgumentOutOfRangeException(nameof(decision));
        }
    }

    private IDecision Decide(string input, bool pack, bool unpack)
    {
        var file = fileSystem.FileInfo.New(input);
        if (file.Exists)
        {
            return
                (pack ? MaybeEncode(file) : null)
                ?? (unpack ? MaybeDecode(file) : null)
                ?? new ErrorDecision("Unsupported file");
        }

        var dir = fileSystem.DirectoryInfo.New(input);
        if (dir.Exists)
        {
            return (pack ? new PackDecision(dir, "Packing directory") : (IDecision?)null)
                   ?? new ErrorDecision("Unsupported directory");
        }

        return new ErrorDecision("Does not exist");
    }

    private IDecision? MaybeEncode(IFileInfo file)
    {
        var descriptor = FormatDescriptors.MatchForEncoding(file.Name);
        log.LogTrace("Maybe encode as {type}?", descriptor.Name);
        return descriptor switch
        {
            {IsContainer: true} => null, // try decode
            TextureDescriptor => new ErrorDecision("Textures can't be encoded by themselves, only packed into peg container"),
            RegularFileDescriptor => null, // try decode
            _ => new PackDecision(file, "Encoding file")
        };
    }

    private IDecision? MaybeDecode(IFileInfo file)
    {
        var descriptor = FormatDescriptors.MatchForDecoding(file.Name);
        log.LogTrace("Maybe decode as {type}?", descriptor.Name);
        return descriptor switch
        {
            TextureDescriptor => new ErrorDecision("Textures can't be decoded by themselves, only unpacked from peg container"),
            RegularFileDescriptor => null, // ignore file
            _ => new UnpackDecision(file, "Unpacking/Decoding file")
        };
    }

    interface IDecision;

    record UnpackDecision(IFileInfo FileInfo, string Message) : IDecision;

    record PackDecision(IFileSystemInfo FileSystemInfo, string Message) : IDecision;

    record ErrorDecision(string Error) : IDecision;
}
