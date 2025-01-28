using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;

namespace RFGM.Archiver.Services;

public class Archiver(IFileSystem fileSystem, FileManager fileManager, Worker worker, ILogger<Archiver> log)
{
    /// <summary>
    /// Pack or unpack items
    /// </summary>
    public async Task ProcessDefault(IReadOnlyList<string> input, CancellationToken token)
    {
        var files = input
                .Where(x => fileSystem.File.Exists(x))
                .Select(x => fileSystem.FileInfo.New(x))
                .Select(x => new UnpackFileMessage(x, Constants.DefaultUnpackDir, UnpackSettings.Default))
                .ToList()
            ;
        var dirs = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Select(x => fileSystem.DirectoryInfo.New(x))
                .Select(x => new PackDirectoryMessage(x, Constants.DefaultPackDir, PackSettings.Default))
                .ToList()
            ;
        if (files.Any() && dirs.Any())
        {
            throw new InvalidOperationException("You specified a mix of files (to unpack) and directories (to pack) at the same time. This is confusing, please do pack and unpack separately");
        }

        if (!files.Any() && !dirs.Any())
        {
            log.LogInformation("Nothing to do: no existing files or directories specified");
            return;
        }

        if (files.Any())
        {
            log.LogInformation("Unpacking files with default settings: {files}", files.Select(x => x.FileInfo.FullName));
            await Unpack(files, Environment.ProcessorCount, token);
            return;
        }

        if (dirs.Any())
        {
            log.LogInformation("Packing directories with default settings: {dirs}", dirs.Select(x => x.DirectoryInfo.FullName));
            await Pack(dirs, Environment.ProcessorCount, token);
            return;
        }
    }

    public async Task Unpack(List<string> input, string output, int parallel, UnpackSettings settings, CancellationToken token)
    {
        var files = input
                .Where(x => fileSystem.File.Exists(x))
                .Select(x => fileSystem.FileInfo.New(x))
                .Select(x => new UnpackFileMessage(x, output, settings))
                .Concat(input
                    .Where(x => fileSystem.Directory.Exists(x))
                    .Select(x => fileSystem.DirectoryInfo.New(x))
                    .SelectMany(x => x.EnumerateFiles())
                    .Select(x => new UnpackFileMessage(x, output, settings))
                )
            ;
        await Unpack(files, parallel, token);
    }

    private async Task Unpack(IEnumerable<UnpackFileMessage> input, int parallel, CancellationToken token)
    {
        await worker.Start(input, parallel, token);
        var logPath = fileSystem.Path.Combine(Environment.CurrentDirectory, ".rfgm.archiver.log");
        if (worker.Failed.Any())
        {
            log.LogError("Failed {count} tasks! Check log for details:\n\t{logPath}", worker.Failed.Count, logPath);
            return;
        }

        var destinations = Destinations
            .Distinct()
            .Select(x => fileSystem.DirectoryInfo.New(x))
            .Where(x => x.Exists)
            .Order()
            .ToList();

        if (Metadata.Any())
        {
            await WriteMetadata(destinations);
        }

        if (destinations.Any())
        {
            log.LogInformation($"See output in:\n\t{string.Join("\n\t", destinations)}");
        }
        else
        {
            log.LogError("No output! Check log for details:\n\t{logPath}", logPath);
        }
    }

    private async Task WriteMetadata(List<IDirectoryInfo> destinations)
    {
        var destination = GetMetadataDestination(destinations);
        fileManager.CreateDirectoryRecursive(destination);
        var file = fileManager.CreateFileRecursive(destination, Constants.MetadataFile, true);
        await using var s = file.Create();
        await using var sw = new StreamWriter(s);
        var collection = Metadata
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Path)
            .ThenBy(x => x.Format)
            .ThenBy(x => x.Hash)
            ;
        await sw.WriteLineAsync(Models.Metadata.ToCsvHeader());
        foreach (var x in collection)
        {
            await sw.WriteLineAsync(x.ToCsv());
        }
        log.LogInformation($"Saved metadata to:\n\t{file}");
    }

    private IDirectoryInfo GetMetadataDestination(List<IDirectoryInfo> destinations)
    {
        switch (destinations.Count)
        {
            case 0:
                log.LogWarning("No output destinations, saving metadata to working directory");
                return fileSystem.DirectoryInfo.New(Environment.CurrentDirectory);
            case 1:
                return destinations.Single();
            default:
                log.LogWarning("Multiple output destinations, saving metadata to first one");
                return destinations.First();
        }
    }

    public static readonly ConcurrentBag<Metadata> Metadata = new();

    public static readonly ConcurrentBag<string> Destinations = new();

    public async Task UnpackMetadata(List<string> input, int parallel, UnpackSettings settings, CancellationToken token)
    {
        var files = input
                .Where(x => fileSystem.File.Exists(x))
                .Select(x => fileSystem.FileInfo.New(x))
                .Select(x => new UnpackFileMessage(x, Constants.DefaultUnpackDir, settings))
                .Concat(input
                    .Where(x => fileSystem.Directory.Exists(x))
                    .Select(x => fileSystem.DirectoryInfo.New(x))
                    .SelectMany(x => x.EnumerateFiles())
                    .Select(x => new UnpackFileMessage(x, Constants.DefaultUnpackDir, settings))
                )
                .ToList()
            ;
        log.LogInformation("Collecting metadata from files: {files}", files.Select(x => x.FileInfo.FullName));
        await Unpack(files, parallel, token);
    }

    public async Task Pack(List<string> input, string output, int parallel, PackSettings settings, CancellationToken token)
    {
        var dirs = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Select(x => fileSystem.DirectoryInfo.New(x))
                .Select(x => new PackDirectoryMessage(x, Constants.DefaultPackDir, settings))
                .ToList()
            ;
        await Pack(dirs, parallel, token);
    }

    private async Task Pack(IEnumerable<PackDirectoryMessage> dirs, int parallel, CancellationToken token)
    {
        await worker.Start(dirs, parallel, token);
        var logPath = fileSystem.Path.Combine(Environment.CurrentDirectory, ".rfgm.archiver.log");
        if (worker.Failed.Any())
        {
            log.LogError("Failed {count} tasks! Check log for details:\n\t{logPath}", worker.Failed.Count, logPath);
            return;
        }

        var destinations = Destinations
            .Distinct()
            .Select(x => fileSystem.DirectoryInfo.New(x))
            .Where(x => x.Exists)
            .Order()
            .ToList();

        if (Metadata.Any())
        {
            await WriteMetadata(destinations);
        }

        if (destinations.Any())
        {
            log.LogInformation($"See output in:\n\t{string.Join("\n\t", destinations)}");
        }
        else
        {
            log.LogError("No output! Check log for details:\n\t{logPath}", logPath);
        }
    }

    public async Task Test(List<string> input, int parallel, CancellationToken token)
    {
        log.LogWarning("You found a hidden [test] command! It does nothing useful, unfortunately, only runs some checks");
        await Task.Yield();

        RunRegex("00001 foo {a=b,c=d}.bar");
        RunRegex("foo {a=b,c=d}.bar");
        RunRegex("foo.bar");

        void RunRegex(string s)
        {
            /*var m = propertyNameFormat.Match(s);
            var ext = m.Groups["ext"].Value;
            var order = m.Groups["order"].Value;
            var nameNoExt = m.Groups["nameNoExt"].Value;
            var props = m.Groups["props"].Value;
            log.LogInformation($"[{s}] = {nameNoExt} {ext} {order} {props}");*/
        }
    }
}
