using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Kaitai;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Archiver.Models.Metadata;
using RFGM.Formats;
using RFGM.Formats.Peg.Models;
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

            await WriteMetadata(fileSystem.DirectoryInfo.New(outputDirs.First()), token);
        }

    }

    public async Task CollectMetadata(List<string> input, bool hash, int parallel, OptimizeFor optimizeFor, CancellationToken token)
    {
        var files = input
            .Where(x => fileSystem.File.Exists(x))
            .Select(x => fileSystem.FileInfo.New(x))
            .ToList();
        var messages1 = FilesToMessages(files, hash, optimizeFor);
        var filesFromDirs = input
                .Where(x => fileSystem.Directory.Exists(x))
                .Select(x => fileSystem.DirectoryInfo.New(x))
                .SelectMany(x => x.EnumerateFiles())
                .ToList();
        var messages2 = FilesToMessages(filesFromDirs, hash, optimizeFor);
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

            await WriteMetadata(fileSystem.DirectoryInfo.New(outputDirs.First()), token);
        }
    }

    private IEnumerable<CollectMetadataMessage> FilesToMessages(IReadOnlyList<IFileInfo> files, bool hash, OptimizeFor optimizeFor)
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
            yield return new CollectMetadataMessage(primary.OpenRead(), secondary?.OpenRead(), name, Breadcrumbs.Init(), hash, optimizeFor);
        }
    }

    public async Task Test(List<string> input, int parallel, CancellationToken token)
    {
        //await TestIndividualCompressed(token);
        //await TestCompactedBlock(token);
        await TestReadByte(token);
        await TestKaitai(token);
    }

    private async Task TestReadByte(CancellationToken token)
    {
        var data = new byte[] {0, 1, 2, 3, 4, 5, 6};
        var ms1 = PrepareCopy(data);
        var tr1 = new TracingStream(ms1, "tr1");
        var b1 = tr1.ReadByte();
        ArgumentOutOfRangeException.ThrowIfNotEqual(b1, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(ms1.Position, 1);

        var ms2 = PrepareCopy(data);
        var tr2 = new StreamView(ms2, 0, ms2.Length, "vw2");
        var b2 = tr2.ReadByte();
        ArgumentOutOfRangeException.ThrowIfNotEqual(b2, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(ms2.Position, 1);

    }

    private async Task TestKaitai(CancellationToken token)
    {
        var cpegFile = fileSystem.FileInfo.New("test/decal_containers.cpeg_pc");
        var fs = cpegFile.OpenRead();
        var ms1 = PrepareCopy(fs);
        var ks1 = new KaitaiStream(ms1);
        //var peg1 = new RfgCpeg(ks1);

        var ms2 = PrepareCopy(fs);
        var tr2 = new TracingStream(ms2, "tr2");
        var ks2 = new KaitaiStream(tr2);
        var peg2 = new RfgCpeg(ks2);

        var ms3 = PrepareCopy(fs);
        var tr3 = new StreamView(ms3, 0, ms3.Length, "vw3");
        var ks3 = new KaitaiStream(tr3);
        var peg3 = new RfgCpeg(ks3);
    }

    private MemoryStream PrepareCopy(Stream s)
    {
        var ms = new MemoryStream();
        s.Seek(0, SeekOrigin.Begin);
        s.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    private MemoryStream PrepareCopy(byte[] s)
    {
        var ms = new MemoryStream(s);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }


    private async Task TestIndividualCompressed(CancellationToken token)
    {
        log.LogInformation("Testing if compressed vpp entries can be read multiple times");
        var vpp = fileSystem.FileInfo.New("test/misc.vpp_pc");
        await using var source = vpp.OpenRead();
        var reader = new VppReader(OptimizeFor.Speed);
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

            var h1 = await Utils.ComputeHash(ms1, token);
            var h2 = await Utils.ComputeHash(ms2, token);
            var h3 = await Utils.ComputeHash(ms3, token);
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
        var reader = new VppReader(OptimizeFor.Speed);
        var archive = await Task.Run(() => reader.Read(source, vpp.Name, token), token);
        await Task.Delay(TimeSpan.FromSeconds(1), token); // let logs flush
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
            ms1.Seek(0, SeekOrigin.Begin);
            ms2.Seek(0, SeekOrigin.Begin);
            var h1 = await Utils.ComputeHash(ms1, token);
            var h2 = await Utils.ComputeHash(ms2, token);
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
            var h3 = await Utils.ComputeHash(ms3, token);
            if (h1 != h3)
            {
                throw new InvalidOperationException($"h1 h3 mismatch ({h1} / {h3})");
            }
        }
    }

    private async Task WriteMetadata(IDirectoryInfo destination, CancellationToken token)
    {
        var file = fileManager.CreateFile(destination, Constants.MetadataFile, true);
        await using var s = file.Create();
        await using var sw = new StreamWriter(s);
        foreach (var x in Metadata.OrderBy(x => x.RelativePath).ThenBy(x => x.GetType().FullName))
        {
            await sw.WriteLineAsync(x.ToString());
        }
        log.LogInformation($"Saved metadata file [{file}]");
    }

    public static readonly ConcurrentBag<IMetadata> Metadata = new();

    public static readonly ConcurrentDictionary<string, byte> StreamTags = new();
}
