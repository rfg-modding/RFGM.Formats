using System.IO.Abstractions;
using Kaitai;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Db;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;
using RFGM.Formats.Abstractions.Descriptors;
using RFGM.Formats.Asm;
using RFGM.Formats.Asm.Models;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class ArchiverDiagnostics(Worker worker, ArchiverState archiverState, AsmReader asmReader, ArchiverContext context, AsmFileLocator asmFileLocator, ILogger<ArchiverDiagnostics> log)
{
    public async Task Run(IDirectoryInfo rfgDataDir, int parallel, CancellationToken token)
    {
        //await PopulateDb(rfgDataDir, token);
        //await LocateFilesForAllPrimitives(rfgDataDir, parallel, token);
        await UpdateAllAsms(rfgDataDir, parallel, token);
    }

    public async Task UpdateAllAsms(IDirectoryInfo rfgDataDir, int parallel, CancellationToken token)
    {
        await context.RebuildTable(context.PrimitiveIndex, token);
        await context.RebuildTable(context.FileIndex, token);
        var vppFiles = rfgDataDir.EnumerateFiles().Where(x => x.Extension.Equals(".vpp_pc", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name).ToList();
        var messages = vppFiles.Select(x => new UpdateAsmMessage(x, rfgDataDir));
        await worker.Start(messages, parallel, false, token);
        await context.PrimitiveIndex.AddRangeAsync(archiverState.PrimitiveIndex, token);
        await context.SaveChangesAsync(token);
        log.LogDebug("Saved PrimitiveIndex table");
        await context.FileIndex.AddRangeAsync(archiverState.FileIndex, token);
        await context.SaveChangesAsync(token);
        log.LogDebug("Saved FileIndex table");
    }

    public async Task LocateFilesForAllPrimitives(IDirectoryInfo rfgDataDir, int parallel, CancellationToken token)
    {
        var messages = await asmFileLocator.InitMessages(rfgDataDir, token);
        try
        {
            await worker.Start(messages, parallel, false, token);
        }
        catch (OperationCanceledException) when(token.IsCancellationRequested)
        {
            // ignore ctrl-c and save results
            token = new CancellationToken();
            log.LogError("Operation not completed! Saving intermediate results...");
        }

        var result = archiverState.PrimitiveLocations.ToList();
        await context.RebuildTable(context.PrimitiveLocations, token);
        await context.PrimitiveLocations.AddRangeAsync(result, token);
        await context.SaveChangesAsync(token);
        log.LogDebug("Saved primitive locations table");
    }

    private async Task PopulateDb(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        await DumpFiles(rfgDataDir, token);
        await DumpDenormalizedAsms(rfgDataDir, token);
        await DumpContainersPrimitives(rfgDataDir, token);
    }

    private async Task DumpDenormalizedAsms(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        var asms = await PrepareAsmIndex(rfgDataDir, token);
        await context.RebuildTable(context.Asm, token);
        await context.Asm.AddRangeAsync(asms, token);
        await context.SaveChangesAsync(token);
        log.LogDebug("Saved asm table");
    }

    private async Task DumpFiles(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        var index = await PrepareFileIndex(rfgDataDir, token);
        await context.RebuildTable(context.Files, token);
        await context.Files.AddRangeAsync(index, token);
        await context.SaveChangesAsync(token);
        log.LogDebug("Saved files table");
    }

    private async Task DumpContainersPrimitives(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        List<NestedItem<LogicalAsm>> asms = new();
        foreach (var file in rfgDataDir.EnumerateFiles().Where(x => x.Extension.Equals(".vpp_pc", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
        {
            var vppReader = new VppReader(OptimizeFor.memory);
            var vpp = vppReader.ReadFile(file, token);
            var asmEntries = vpp.LogicalFiles.Where(x => x.Name.EndsWith(AsmDescriptor.Extension)).OrderBy(x => x.Name).ToList();
            var items = asmEntries.Select(x => asmReader.Read(x.Content, x.Name, token)).Select(x => new NestedItem<LogicalAsm>(Breadcrumbs.Init().Descend(vpp.Name), x));
            asms.AddRange(items);
        }

        var containers = asms.Select(x => x.Item.Containers.Select(y => new NestedItem<LogicalContainer>(x.Breadcrumbs.Descend(x.Item.Name), y)))
            .SelectMany(x => x)
            .ToList();
        log.LogDebug("Total containers: [{x}]", containers.Count);
        var uniq = 0;
        var dup = 0;
        var wtf = 0;
        var groups = containers.GroupBy(x => x.Item.Name).ToList();
        foreach (var g in groups)
        {
            var items = g.ToList();
            if (items.Count == 1)
            {
                uniq++;
                continue;
            }

            var sample = items.First();
            var s = sample.Item;
            var duplicates = items.Skip(1).Where(x => x.Item.DataOffset == s.DataOffset
                                                      && x.Item.Primitives.Count == s.Primitives.Count
                                                      && x.Item.Flags == s.Flags
                                                      && x.Item.Type == s.Type
                                                      && x.Item.CompressedSize == s.CompressedSize
                                                      && x.Item.IsVirtual == s.IsVirtual
                                                      && x.Item.Primitives.Zip(s.Primitives).All(y => y.First == y.Second)
                )
                .ToList();
            if(duplicates.Count > 0)
            {
                dup += duplicates.Count;

            }

            if (duplicates.Count + 1 != items.Count)
            {
                wtf += items.Count;
            }
        }

        log.LogDebug("Containers: {a} total, {b} uniq, {c} dup, {d} wtf", containers.Count, uniq, dup, wtf);

        var c = containers.Select(x => new ContainerEntry
        {
            Id = x.Breadcrumbs.Descend(x.Item.Name).ToString(),
            AsmId = x.Breadcrumbs.ToString(),
            Name = x.Item.Name.ToLowerInvariant(),
            ContainerType = x.Item.Type.ToString().ToLowerInvariant(),
            ContainerFlags = x.Item.Flags.ToString().ToLowerInvariant(),
            DataOffset = x.Item.DataOffset,
            CompressedDataLength = x.Item.CompressedSize,
            PrimitiveCount = x.Item.Primitives.Count,
            Primitives = string.Join(',', x.Item.Primitives.Select(p => p.Name.ToLowerInvariant())),
            IsVirtual = x.Item.IsVirtual,
        });

        await context.RebuildTable(context.Containers, token);
        await context.Containers.AddRangeAsync(c, token);
        await context.SaveChangesAsync(token);
        log.LogDebug("Saved containers table");

        var p = containers.Select(x => x.Item.Primitives.Select(y => new NestedItem<LogicalPrimitive>(x.Breadcrumbs.Descend(x.Item.Name), y)))
            .SelectMany(x => x)
            .Select(x => new PrimitiveEntry
            {
                Id = x.Breadcrumbs.Descend(x.Item.Name).ToString(),
                ContainerId = x.Breadcrumbs.ToString(),
                Name = x.Item.Name.ToLowerInvariant(),
                PrimitiveType = x.Item.Type.ToString().ToLowerInvariant(),
                PrimitiveFlags = x.Item.Flags.ToString().ToLowerInvariant(),
                Allocator = x.Item.Allocator.ToString().ToLowerInvariant(),
                SplitExtIndex = x.Item.Split,
                CpuSize = x.Item.CpuSize,
                GpuSize = x.Item.GpuSize,
            });
        await context.RebuildTable(context.Primitives, token);
        await context.Primitives.AddRangeAsync(p, token);
        await context.SaveChangesAsync(token);
        log.LogDebug("Saved primitives table");
    }

    private async Task<List<FileEntry>> PrepareFileIndex(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        List<FileEntry> index = new();
        var vppReader = new VppReader(OptimizeFor.memory);
        foreach (var vppFile in rfgDataDir.EnumerateFiles().Where(x => x.Extension.Equals(".vpp_pc", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
        {
            await using var f = vppFile.OpenRead();
            var s = new KaitaiStream(f);
            var rawVpp = new RfgVpp(s);
            var blockOffset = rawVpp.BlockOffset;
            f.Seek(0, SeekOrigin.Begin);
            var vpp = vppReader.ReadFile(vppFile, token);
            foreach (var logicalFile in vpp.LogicalFiles)
            {
                var (name, ext) = SplitNameAndFullExt(logicalFile.Name);
                var path = string.Join('/', vpp.Name, logicalFile.Name);
                var idx = new FileEntry
                {
                    Id = path,
                    Vpp = vpp.Name.ToLowerInvariant(),
                    Str = null,
                    File = logicalFile.Name.ToLowerInvariant(),
                    StrName = null,
                    FileName = name.ToLowerInvariant(),
                    FileExt = ext.ToLowerInvariant(),
                    DataOffset = blockOffset,
                    FileIndex = logicalFile.Order,
                    FileOffset = logicalFile.Offset,
                    FileSize = logicalFile.Content.Length,
                };
                index.Add(idx);

                if (logicalFile.Name.EndsWith(".str2_pc", StringComparison.OrdinalIgnoreCase))
                {
                    ReadStr2(logicalFile, vpp, blockOffset);
                }
            }
        }

        return index;

        void ReadStr2(LogicalFile logicalFile, LogicalArchive vpp, int blockOffset)
        {
            var str = vppReader.Read(logicalFile.Content, logicalFile.Name, token);
            foreach (var x in str.LogicalFiles.OrderBy(x => x.Name))
            {
                var (name, ext) = SplitNameAndFullExt(x.Name);
                var path = string.Join('/', vpp.Name, str.Name, x.Name);
                var idx = new FileEntry
                {
                    Id = path,
                    Vpp = vpp.Name.ToLowerInvariant(),
                    Str = str.Name.ToLowerInvariant(),
                    File = x.Name.ToLowerInvariant(),
                    StrName = SplitNameAndFullExt(str.Name).name.ToLowerInvariant(),
                    FileName = name.ToLowerInvariant(),
                    FileExt = ext.ToLowerInvariant(),
                    DataOffset = blockOffset,
                    FileIndex = x.Order,
                    FileOffset = x.Offset,
                    FileSize = x.Content.Length,
                };

                index.Add(idx);
            }
        }
    }

    private async Task<List<AsmEntry>> PrepareAsmIndex(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        var asms = new List<AsmEntry>();
        foreach (var file in rfgDataDir.EnumerateFiles().Where(x => x.Extension.Equals(".vpp_pc", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
        {
            var vppReader = new VppReader(OptimizeFor.memory);
            var vpp = vppReader.ReadFile(file, token);
            var asmEntries = vpp.LogicalFiles.Where(x => x.Name.EndsWith(AsmDescriptor.Extension)).OrderBy(x => x.Name).ToList();
            foreach (var asmEntry in asmEntries)
            {
                var asm = asmReader.Read(asmEntry.Content, asmEntry.Name, token);
                foreach (var c in asm.Containers)
                {
                    var (cName, cExt) = SplitNameAndFullExt(c.Name);
                    foreach (var p in c.Primitives)
                    {
                        var (pName, pExt) = SplitNameAndFullExt(p.Name);
                        var path = string.Join('/', vpp.Name, asm.Name, c.Name, p.Name);
                        var entry = new AsmEntry
                        {
                            Id = path,
                            Vpp = vpp.Name,
                            AsmName = asm.Name.ToLowerInvariant(),
                            Container = c.Name.ToLowerInvariant(),
                            ContainerName = cName.ToLowerInvariant(),
                            ContainerExt = cExt.ToLowerInvariant(),
                            Primitive = p.Name.ToLowerInvariant(),
                            PrimitiveName = pName.ToLowerInvariant(),
                            PrimitiveExt = pExt.ToLowerInvariant(),
                            DataOffset = c.DataOffset,
                            CompressedDataLength = c.CompressedSize,
                            SplitExtIndex = p.Split,
                            CpuSize = p.CpuSize,
                            GpuSize = p.GpuSize,
                            ContainerFlags = c.Flags.ToString().ToLowerInvariant(),
                            ContainerType = c.Type.ToString().ToLowerInvariant(),
                            Allocator = p.Allocator.ToString().ToLowerInvariant(),
                            PrimitiveFlags = p.Flags.ToString().ToLowerInvariant(),
                            PrimitiveType = p.Type.ToString().ToLowerInvariant(),
                            IsVirtual = c.IsVirtual
                        };
                        asms.Add(entry);
                    }
                }

            }
        }

        return asms;
    }

    private static (string name, string ext) SplitNameAndFullExt(string value)
    {
        var tokens = value.Split('.');
        var name = tokens.First();
        var ext = tokens.Length > 0 ? string.Join('.', tokens.Skip(1)) : string.Empty;
        return (name, ext);
    }
}
