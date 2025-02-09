using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Commandline;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;

namespace RFGM.Archiver.Services;

public class Archiver(IFileSystem fileSystem, ArchiverState archiverState, Worker worker, Rick rick,IServiceScopeFactory serviceScopeFactory,  ILogger<Archiver> log)
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

    public async Task<ExitCode> CommandDiagnostics(List<string> input, int parallel, CancellationToken token)
    {
        log.LogWarning("You found hidden [{name}] command! It does nothing useful, unfortunately, only runs some checks", nameof(Diagnostics));
        var rfgPath = input.FirstOrDefault() ?? @"C:\Program Files (x86)\Steam\steamapps\common\Red Faction Guerrilla Re-MARS-tered\data";
        var rfgDataDir = fileSystem.DirectoryInfo.New(rfgPath);
        log.LogInformation("Using RFG path: [{}]", rfgDataDir.FullName);
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var archiverDiagnostics = scope.ServiceProvider.GetRequiredService<ArchiverDiagnostics>();
        await archiverDiagnostics.Run(rfgDataDir, parallel, token);
        return ExitCode.Ok;
    }

    private async Task<ExitCode> Process(IEnumerable<IMessage> messages, int parallel, bool checkOutput, CancellationToken token)
    {
        await worker.Start(messages, parallel, true, token);
        var logPath = fileSystem.Path.Combine(AppContext.BaseDirectory, ".rfgm.archiver.log");
        if (worker.Failed.Any())
        {
            log.LogError("Failed {count} tasks! Check log for details:\n\t{logPath}", worker.Failed.Count, logPath);
            return ExitCode.FailedTasks;
        }
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var metadataWriter = scope.ServiceProvider.GetRequiredService<MetadataWriter>();
        await metadataWriter.WriteCsv(token);
        await metadataWriter.WriteSqlite(token);
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
