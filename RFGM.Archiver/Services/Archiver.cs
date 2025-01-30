using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Services;

public class Archiver(IFileSystem fileSystem, MetadataWriter metadataWriter, ArchiverState archiverState, Worker worker, Rick rick, ILogger<Archiver> log)
{
    /// <summary>
    /// Pack or unpack items from drag-and-drop on .exe
    /// </summary>
    public async Task<ExitCode> CommandDefault(IReadOnlyList<string> input, bool force, CancellationToken token)
    {
        if (input.Any(x => x.EndsWith("rfg.exe", StringComparison.OrdinalIgnoreCase)))
        {
            rick.Roll();
            return ExitCode.Rick;
        }

        var messages = input.Select(x => new DecideMessage(x, null, PackSettings.Default with {Force = force}, UnpackSettings.Default with {Force = force}));
        return await Process(messages, Environment.ProcessorCount, true, token);
    }

    public async Task<ExitCode> CommandUnpack(List<string> input, string output, int parallel, UnpackSettings settings, CancellationToken token)
    {
        var messages = input
                .Where(x => fileSystem.File.Exists(x))
                .Concat(input
                    .Where(x => fileSystem.Directory.Exists(x))
                    .Select(x => fileSystem.DirectoryInfo.New(x))
                    .SelectMany(x => x.EnumerateFiles())
                    .Select(x => x.FullName)
                )
                .Select(x => new DecideMessage(x, output, null, settings))
            ;
        return await Process(messages, parallel, true, token);
    }

    public async Task<ExitCode> CommandMetadata(List<string> input, int parallel, UnpackSettings settings, CancellationToken token)
    {
        var messages = input
                .Where(x => fileSystem.File.Exists(x))
                .Concat(input
                    .Where(x => fileSystem.Directory.Exists(x))
                    .Select(x => fileSystem.DirectoryInfo.New(x))
                    .SelectMany(x => x.EnumerateFiles())
                    .Select(x => x.FullName)
                )
                .Select(x => new DecideMessage(x, null, null, settings))
            ;
        return await Process(messages, parallel, false, token);
    }

    public async Task<ExitCode> CommandPack(List<string> input, string output, int parallel, PackSettings settings, CancellationToken token)
    {
        var messages = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Concat(input
                    .Where(x => fileSystem.File.Exists(x))
                )
                .Select(x => new DecideMessage(x, output, settings, null))
            ;
        return await Process(messages, parallel, true, token);
    }

    public async Task<ExitCode> CommandTest(List<string> input, int parallel, CancellationToken token)
    {
        log.LogWarning("You found a hidden [test] command! It does nothing useful, unfortunately, only runs some checks");
        await Task.Yield();

        return 0;
    }

    private async Task<ExitCode> Process(IEnumerable<IMessage> messages, int parallel, bool checkOutput, CancellationToken token)
    {
        await worker.Start(messages, parallel, token);
        var logPath = fileSystem.Path.Combine(Environment.CurrentDirectory, ".rfgm.archiver.log");
        if (worker.Failed.Any())
        {
            log.LogError("Failed {count} tasks! Check log for details:\n\t{logPath}", worker.Failed.Count, logPath);
            return ExitCode.FailedTasks;
        }

        await metadataWriter.Write(token);
        if (!checkOutput)
        {
            return ExitCode.Ok;
        }

        var destinations = archiverState.GetDestinations();
        if (destinations.Any())
        {
            log.LogInformation($"See output in:\n\t{string.Join("\n\t", destinations)}");
            return ExitCode.Ok;
        }

        log.LogError("No output! Check log for details:\n\t{logPath}", logPath);
        return ExitCode.NoOutput;
    }
}
