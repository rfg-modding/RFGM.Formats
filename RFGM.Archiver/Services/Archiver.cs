using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Formats;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;

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
                .Select(x => UnpackMessage.Default(x, fileSystem))
                .ToList()
            ;
        var dirs = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Select(x => fileSystem.DirectoryInfo.New(x))
                .Select(x => PackMessage.Default(x, fileSystem))
                .ToList()
            ;
        if (files.Any() && dirs.Any())
        {
            throw new InvalidOperationException("You specified a mix of files (to unpack) and folders (to pack) at the same time. This is confusing, please do pack and unpack separately");
        }

        if (!files.Any() && !dirs.Any())
        {
            log.LogInformation("Nothing to do: no existing files or folders specified");
            return;

        }

        if (files.Any())
        {
            log.LogInformation("Unpacking files with default settings: {files}", files.Select(x => x.Source.FullName));
            await worker.Start(files, Environment.ProcessorCount, token);
        }

        if (dirs.Any())
        {
            log.LogInformation("Packing dirs with default settings: {dirs}", dirs.Select(x => x.Source.FullName));
            await worker.Start(dirs, Environment.ProcessorCount, token);
        }

        if (worker.Failed.Any())
        {
            throw new InvalidOperationException($"Failed {worker.Failed.Count} tasks. Check logs for details");
        }

        if (Metadata.Any())
        {
            var outputDirs = files.Select(x => x.OutputPath.FullName).Concat(dirs.Select(x => x.OutputPath.FullName)).Distinct().ToList();
            if (outputDirs.Count > 1)
            {
                log.LogWarning("Multiple output dirs! Saving metadata to first one");
            }

            await WriteMetadata(token, fileSystem.DirectoryInfo.New(outputDirs.First()));
        }

    }

    public async Task CollectMetadata(List<string> input, bool hash, int parallel, CancellationToken token)
    {
        var files = input
            .Where(x => fileSystem.File.Exists(x))
            .Select(x => fileSystem.FileInfo.New(x))
            .ToList();
        var messages1 = FilesToMessages(files, hash);
        var filesFromDirs = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Select(x => fileSystem.DirectoryInfo.New(x))
                .SelectMany(x => x.EnumerateFiles())
                .ToList();
        var messages2 = FilesToMessages(filesFromDirs, hash);
        var all = messages1.Concat(messages2).ToList();
        log.LogInformation("Collecting metadata for: {all}", all.Select(x => x.Name));
        await worker.Start(all, parallel, token);

        if (worker.Failed.Any())
        {
            throw new InvalidOperationException($"Failed {worker.Failed.Count} tasks. Check logs for details");
        }

        if (Metadata.Any())
        {
            var outputDirs = files.Concat(filesFromDirs).Select(x => x.Directory!.FullName).Distinct().ToList();
            if (outputDirs.Count > 1)
            {
                log.LogWarning("Multiple output dirs! Saving metadata to first one");
            }

            await WriteMetadata(token, fileSystem.DirectoryInfo.New(outputDirs.First()));
        }
    }

    private IEnumerable<CollectMetadataMessage> FilesToMessages(IReadOnlyList<IFileInfo> files, bool hash)
    {
        foreach (var file in files)
        {
            var maybePair = PairedFiles.FromExistingFile(file);
            // ignore pairs built from secondary files to avoid duplicates
            if (maybePair != null && maybePair.Gpu.FullName == file.FullName)
            {
                log.LogTrace($"Skipped secondary file [{file.FullName}]");
                continue;
            }

            var primary = maybePair?.Cpu ?? file;
            var secondary = maybePair?.Gpu;
            var name = Utils.GetNameWithoutNumber(maybePair?.Name ?? file.Name);
            yield return new CollectMetadataMessage(primary.OpenRead(), secondary?.OpenRead(), name, Breadcrumbs.Init(), hash);
        }
    }

    public async Task Test(List<string> input, int parallel, CancellationToken token)
    {
        await TestIndividualCompressed(token);
        await TestCompactedBlock(token);
    }

    private async Task TestIndividualCompressed(CancellationToken token)
    {
        log.LogInformation("Testing if compressed vpp entries can be read multiple times");
        var vpp = fileSystem.FileInfo.New("test/misc.vpp_pc");
        await using var source = vpp.OpenRead();
        var reader = new VppReader();
        var archive = await Task.Run(() => reader.Read(source, vpp.Name, token), token);
        log.LogInformation("{archive}", archive);
        foreach (var entry in archive.LogicalFiles)
        {
            var x = entry.Content;


            var ms1 = new MemoryStream();
            var ms2 = new MemoryStream();
            var ms3 = new MemoryStream();
            await x.CopyToAsync(ms1, token);
            ms1.Seek(0, SeekOrigin.Begin);
            x.Seek(0, SeekOrigin.Begin);
            await x.CopyToAsync(ms2, token);
            ms2.Seek(0, SeekOrigin.Begin);

            x.Seek(0, SeekOrigin.Begin);
            x.Seek(10, SeekOrigin.Begin);
            x.Seek(-5, SeekOrigin.Current);
            x.Seek(5, SeekOrigin.Current);
            x.Seek(-5, SeekOrigin.End);
            x.Seek(0, SeekOrigin.Begin);
            await x.CopyToAsync(ms3, token);
            ms3.Seek(0, SeekOrigin.Begin);

            var h1 = await Utils.ComputeHash(ms1);
            var h2 = await Utils.ComputeHash(ms2);
            var h3 = await Utils.ComputeHash(ms3);
            if (h1 != h2 || h1 != h3)
            {
                throw new InvalidOperationException($"Different hashes for {entry}");
            }
        }
    }

    private async Task TestCompactedBlock(CancellationToken token)
    {
        log.LogInformation("Testing if compacted vpp entries can be read multiple times");
        var vpp = fileSystem.FileInfo.New("test/table.vpp_pc");
        await using var source = vpp.OpenRead();
        var reader = new VppReader();
        var archive = await Task.Run(() => reader.Read(source, vpp.Name, token), token);
        await Task.Delay(TimeSpan.FromSeconds(1), token); // let logs flush
        foreach (var entry in archive.LogicalFiles)
        {
            //Console.WriteLine("=========================================================");
            //Console.WriteLine(entry);
            var x = entry.Content;
            var ms1 = new MemoryStream();
            var ms2 = new MemoryStream();
            var ms3 = new MemoryStream();

            //Console.WriteLine($"* clean\n\ttop level ={x}");
            await x.CopyToAsync(ms1, token);
            //Console.WriteLine($"* after copy 1\n\ttop level ={x}");
            ms1.Seek(0, SeekOrigin.Begin);
            x.Seek(0, SeekOrigin.Begin);
            //Console.WriteLine($"* rewind\n\ttop level ={x}");
            await x.CopyToAsync(ms2, token);
            //Console.WriteLine($"* after copy 2\n\ttop level ={x}");
            ms2.Seek(0, SeekOrigin.Begin);
            //Console.WriteLine($"ms1=[{ms1.ReadAsciiString(30)}]");
            //Console.WriteLine($"ms2=[{ms2.ReadAsciiString(30)}]");
            ms1.Seek(0, SeekOrigin.Begin);
            ms2.Seek(0, SeekOrigin.Begin);
            var h1 = await Utils.ComputeHash(ms1);
            var h2 = await Utils.ComputeHash(ms2);
            if (h1 != h2)
            {
                throw new InvalidOperationException($"h1 h2 mismatch ({h1} / {h2})");
            }

            x.Seek(0, SeekOrigin.Begin);
            x.Seek(10, SeekOrigin.Begin);
            x.Seek(-5, SeekOrigin.Current);
            x.Seek(5, SeekOrigin.Current);
            x.Seek(-5, SeekOrigin.End);
            x.Seek(0, SeekOrigin.Begin);
            await x.CopyToAsync(ms3, token);
            ms3.Seek(0, SeekOrigin.Begin);
            var h3 = await Utils.ComputeHash(ms3);
            if (h1 != h3)
            {
                throw new InvalidOperationException($"h1 h3 mismatch ({h1} / {h3})");
            }
        }
    }

    private async Task WriteMetadata(CancellationToken token, IDirectoryInfo destination)
    {
        var file = fileManager.CreateFile(destination, Constants.MetadataFile, true);
        await using var s = file.Create();
        await using var sw = new StreamWriter(s);
        foreach (var x in Metadata.OrderBy(x => x.RelativePath))
        {
            await sw.WriteLineAsync(x.ToString());
        }
        log.LogInformation($"Saved metadata file [{file}]");
    }

    public static readonly ConcurrentBag<IMetadata> Metadata = new();

    public static readonly ConcurrentDictionary<string, byte> StreamTags = new();
}
