using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using RFGM.Formats;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Abstractions.Descriptors;
using RFGM.Formats.Asm;
using RFGM.Formats.Asm.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class AsmUpdater(AsmReader asmReader, ArchiverState archiverState, FileManager fileManager, ILogger<AsmUpdater> log)
{
    private string? storedVppName;

    public async Task Update(IFileSystemInfo vpp, IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        if (!vpp.Name.Contains("interface.vpp_pc"))
        {
            // TODO REMOVE ME
            //return;
        }

        var vppName = FormatDescriptors.RegularFile.ReadEntryForEncoding(vpp).Name;
        storedVppName = vppName;
        var current = ReadAllAsmFromVpp(vpp, token);
        var all = await ReadAllAsmFromAllVpp(rfgDataDir, token);
        var forUpdate = GetVppAndAsmForUpdate(current, all).ToList();
        log.LogInformation("Vpp [{}], found {} asm files to update:\n\t{}", vpp.Name, forUpdate.Count, string.Join("\n\t", forUpdate));

        var containers = IndexContainers(forUpdate).ToList();
        var strs = IndexStrs(vpp, rfgDataDir, forUpdate.Select(x => x.Vpp), token).ToList();
        SaveResults(vppName, containers, strs, token);

        var newAsms = new Dictionary<(string, string), IReadOnlyList<Container>>();
        var asmsInVpp = containers.GroupBy(x => x.Vpp).ToList();
        foreach (var x in asmsInVpp)
        {
            var newContainers = Update(vppName, x, strs, all, token).GroupBy(x => (x.Vpp, x.Asm));
            foreach (var y in newContainers)
            {
                newAsms.Add(y.Key, y.ToList());

            }
        }

        foreach (var (archive, asmForUpd) in forUpdate)
        {
            foreach (var asm in asmForUpd)
            {
                var key = (archive, asm.Name);
                if (!newAsms.Remove(key, out var upd))
                {
                    StoreUpdate($"{archive}/{asm.Name}", "No updates");
                    continue;
                }

                var updAsm = new LogicalAsm(upd.Select(c =>
                    new LogicalContainer(c.Name, c.Type, c.Flags, c.Offset, c.CompressedSize, c.IsVirtual, c.Primitives.Select(p =>
                        new LogicalPrimitive(p.Name, p.Type, p.Allocator, p.Flags, p.Split, p.CpuSize, p.GpuSize)
                    ).ToList())
                ).ToList(), asm.Name);

                var origFile = fileManager.CreateFileRecursive(vpp.FileSystem.Path.Join(Constants.DefaultPackDir, vppName, archive, asm.Name + ".bak.json"), true);
                await using var of = origFile.OpenWrite();
                await new AsmWriter(asm).WriteJson(of, token);

                var jsonFile = fileManager.CreateFileRecursive(vpp.FileSystem.Path.Join(Constants.DefaultPackDir, vppName, archive, asm.Name + ".upd.json"), true);
                await using var uf = jsonFile.OpenWrite();
                await new AsmWriter(updAsm).WriteJson(uf, token);
                StoreUpdate($"{archive}/{asm.Name}", $"Saved updated file: {jsonFile.FullName}");
            }
        }

/*
find asm container for every str2
compare offsets, sizes, primitive order, etc
update if possible
    primitive size change
    offset change
    compressed size change
    primitive name change?
    new primitive
    if moved from elsewhere, just copy all the attributes
    deleted primitive
    ask user to intervene eg if new and unknown
find asm primitive everywhere for every separate file
    only look in virtual asms
    look only in certain vpps
    dont forget files directly in vpp
output to .pack directory

approach 1
match primitives to files across allVpps
because some files are not in changedVpp, they are in other places
then compare with actual files only in changed vpp
figure out if file exists in several places but is deleted in changedVpp - is it fine to delete file which exists in other places?
how to handle new files?

approach 2
match all files in changedVpp to primitives
primitives without match: look in allVpps
delete primitives without match (file was deleted)
update primitives with different size
add primitives for new files
    find similar files in all asms
    if not found, warn user to fill parameters


every str2 maps to a container
deal with virtual containers separately


compare stuff

non-virtual containers
    list of containers <=> list of str2s
        * how to compare: fill all asms, then see if any dirty files have no match
        * name match, c != str2 => compare primitives, update container fields
        * c == null => new or copied asset, scan index, find closest match or create new, compare primitives, update container fields
        * str2 == null => delete container

    list of primitives <=> list of files
        * how to compare: fill all asms, then see if any dirty files have no match
        * name match, p != f => compare and update fields
        * p == null => new or copied asset, scan index, find closest match or create new, compare and update fields
        * f == null => delete primitive

*/
    }

    private void StoreUpdate(string id, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedVppName);
        archiverState.StoreUpdate(storedVppName, id, text);
    }

    private IReadOnlyList<Container> Update(string vppName, IGrouping<string, Container> containers, IReadOnlyList<Str> strs, IReadOnlyList<VppAndAsm> all, CancellationToken token)
    {
        /*fill non-virtual containers:
         - only in current vpp: non-virtuals can only reference files in their own vpp
         - ignore root vpp files: non-virtuals can only reference str2
         */
        /* ===========================================================================
         * TODO work with containers sequentially to preserve order
         * TODO handle virtual
         * ===========================================================================
         */
        var upd = new List<Container>();
        var foreignVpp = !vppName.Equals(containers.Key, StringComparison.OrdinalIgnoreCase);
        var realStrs = strs
            .Where(x => x.Vpp.Equals(vppName, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Name != FakeStr2Root)
            .ToDictionary(x => x.Name[..^8], StringComparer.OrdinalIgnoreCase);

        foreach (var container in containers)
        {
            if (container.IsVirtual)
            {
                // TODO process virtual
                StoreUpdate(container.Id, $"TODO work with virtual");
                upd.Add(container);
            }
            else
            {
                if (foreignVpp)
                {
                    StoreUpdate(container.Id, $"Copying real container in foreign vpp");
                    upd.Add(container);
                    continue;
                }

                if (!realStrs.Remove(container.Name, out var str))
                {
                    StoreUpdate(container.Id, $"Delete because no matching real str found");
                }
                else
                {
                    upd.Add(UpdateNonVirtual(container, str, all, token));
                }
            }
        }

        if (!foreignVpp)
        {
            if (realStrs.Any())
            {
                var asmToAppend = containers.Select(x => x.Asm).Distinct().Order().ToList();
                if (asmToAppend.Count == 0)
                {
                    throw new InvalidOperationException("Can't add new str2s: no asm files");

                }
                if (asmToAppend.Count > 1)
                {
                    StoreUpdate($"{vppName}/{containers.Key}", $"WARNING: Vpp has {asmToAppend.Count} ASM files, will add {realStrs.Count} new strs to first one. Fix result manually!");
                }

                var asm = asmToAppend.First();
                StoreUpdate($"{vppName}/{containers.Key}", $"Add {realStrs.Count} new real strs as containers");
                foreach (var x in realStrs)
                {
                    var c = AddNonVirtual(x.Value, all, asm, token);
                    UpdateNonVirtual(c, x.Value, all, token);
                    upd.Add(c);
                }
            }

        }

        return upd;
    }

    private Container AddNonVirtual(Str str, IReadOnlyList<VppAndAsm> all, string asm, CancellationToken token)
    {
        // find anything that looks like new str
        // TODO ignore virtual when looking for a non-virtual file?
        var name = str.Name[..^8];
        List<(LogicalContainer c, int score)> result = new();
        foreach (var (vpp, asms) in all)
        {
            token.ThrowIfCancellationRequested();
            foreach (var a in asms)
            {
                token.ThrowIfCancellationRequested();
                foreach (var c in a.Containers)
                {
                    token.ThrowIfCancellationRequested();
                    var score = 0;
                    if (c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 4;
                    }

                    // don't bother to compare contents here, just update them later
                    if (c.Primitives.Count == str.Files.Count)
                    {
                        score += 2;
                    }

                    if (c.DataOffset == str.Offset)
                    {
                        score += 1;
                    }

                    if (c.CompressedSize == str.CompressedSize)
                    {
                        score += 1;
                    }

                    result.Add((c, score));

                }
            }
        }

        var topResults = result.GroupBy(x => x.score).MaxBy(x => x.Key);
        var maxScore = topResults?.Key;
        var cs = topResults?.Select(x => x.c).ToList();
        if (cs?.Count is null or 0)
        {
            StoreUpdate(str.Id, $"WARNING: can't find any container similar to {str}, using stub. You must set container fields manually!");
            return new Container(str.Vpp, asm, name, -1, str.Offset, str.CompressedSize, false, [], RfgAsm.Container.ContainerType.None, RfgAsm.Container.ContainerFlags.None);

        }

        if (maxScore != 8)
        {
            StoreUpdate(str.Id, $"WARNING: found containers similar to {str}, but not exactly. Check result for correctness!");
        }

        if (cs.Count > 1)
        {
            StoreUpdate(str.Id, $"WARNING: found {cs.Count} containers similar to {str}, using first. Check result for correctness!");
        }

        var top = cs.First();
        StoreUpdate(str.Id, $"Found {cs.Count} containers similar to {str}: score {maxScore}, top {top}");
        var container = new Container(str.Vpp, asm, name, -1, str.Offset, str.CompressedSize, top.IsVirtual, [], top.Type, top.Flags);
        foreach (var p in top.Primitives)
        {
            container.Primitives.Add(new Primitive(container, p.Name, -1, p.CpuSize, p.GpuSize, p.Type, p.Allocator, p.Flags, p.Split));
        }
        return container;
    }

    private Container UpdateNonVirtual(Container container, Str str, IReadOnlyList<VppAndAsm> all, CancellationToken token)
    {
        var leftOuterJoin = container.Primitives
            .GroupJoin(str.Files, p => (p.Order, AsmHelper.FixableVanillaNames.GetValueOrDefault(p.Name, p.Name).ToLowerInvariant()), f => (f.FakeOrder, f.Name.ToLowerInvariant()), (p, fs) => (p, fs))
            .Select(x => (x.p, x.fs.SingleOrDefault()))
            .ToDictionary(x => x.p, x => x.Item2);
        var matched = leftOuterJoin.Values.ToHashSet();

        var updatePairs = leftOuterJoin.Where(x => x.Value is not null).ToList();
        // let's just do nothing with delete list and hope that order will be correct after that
        var deletePrimitives = leftOuterJoin.Where(x => x.Value is null).Select(x => x.Key).ToList();
        var addFiles = str.Files.Where(x => !matched.Contains(x)).ToList();
        StoreUpdate(container.Id, $"Comparing {container.Primitives.Count} non-virtual primitives vs {str.Files.Count} str2 in {str.Vpp}");

        var updContainer = container with{Primitives = []};
        UpdateNonVirtual(updContainer, updatePairs, token);
        AddNonVirtual(updContainer, addFiles, all, token);

        StoreUpdate(container.Id, $"Delete {deletePrimitives.Count} primitives:\n" + string.Join("\n", deletePrimitives));

        if (updContainer.Offset != str.Offset)
        {
            if (str.Offset == -1)
            {
                throw new InvalidOperationException("Attempt to update container Offset to -1");
            }

            StoreUpdate(container.Id, "Update container Offset");
            updContainer = updContainer with {Offset = str.Offset};
        }

        if (updContainer.CompressedSize != str.CompressedSize)
        {
            if (str.CompressedSize == -1)
            {
                throw new InvalidOperationException("Attempt to update container CompressedSize to -1");
            }

            StoreUpdate(container.Id, "Update container CompressedSize");
            updContainer = updContainer with {CompressedSize = str.CompressedSize};
        }

        if (updContainer.Count == 0)
        {
            StoreUpdate(updContainer.Id, "WARNING: Container is empty after updates. Remove str2 instead?");
        }

        return updContainer;
    }

    private void UpdateNonVirtual(Container container, List<KeyValuePair<Primitive, File?>> updatePair, CancellationToken token)
    {
        foreach (var (p, f) in updatePair)
        {
            token.ThrowIfCancellationRequested();
            if (f is null)
            {
                throw new InvalidOperationException("Expected primitive-file pair");
            }

            var updPrimitive = p with{Parent = container};
            StoreUpdate(updPrimitive.Id, $"Comparing with {f}");
            if (updPrimitive.CpuSize != f.CpuSize)
            {
                StoreUpdate(updPrimitive.Id, $"Update CpuSize");
                updPrimitive = updPrimitive with {CpuSize = f.CpuSize};
            }

            if (updPrimitive.GpuSize != (f.GpuSize ?? 0))
            {
                StoreUpdate(updPrimitive.Id, $"Update GpuSize");
                if (f.GpuSize is null)
                {
                    throw new InvalidOperationException("Attempt to update primitive GpuSize to null");
                }

                updPrimitive = updPrimitive with {GpuSize = f.GpuSize.Value};
            }

            container.Primitives.Add(updPrimitive);
        }
    }

    private void AddNonVirtual(Container container, List<File> addFiles, IReadOnlyList<VppAndAsm> all, CancellationToken token)
    {
        // find anything that looks like new files
        // TODO ignore virtual when looking for a non-virtual file?
        foreach (var file in addFiles)
        {
            token.ThrowIfCancellationRequested();
            List<(LogicalPrimitive p, int score)> result = new();
            var name = AsmHelper.FixableVanillaNames.GetValueOrDefault(file.Name, file.Name);
            foreach (var (vpp, asms) in all)
            {
                token.ThrowIfCancellationRequested();
                foreach (var a in asms)
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var c in a.Containers)
                    {
                        token.ThrowIfCancellationRequested();
                        foreach (var p in c.Primitives)
                        {
                            token.ThrowIfCancellationRequested();
                            var score = 0;
                            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                score += 2;
                            }

                            if (p.CpuSize == file.CpuSize)
                            {
                                score += 1;
                            }
                            if (p.GpuSize == (file.GpuSize ?? 0))
                            {
                                score += 1;
                            }
                            result.Add((p , score));
                        }
                    }
                }
            }

            var topResults = result.GroupBy(x => x.score).MaxBy(x => x.Key);


            var maxScore = topResults?.Key;
            var ps = topResults?.Select(x => x.p).ToList();
            if (ps?.Count is null or 0)
            {
                StoreUpdate(container.Id, $"WARNING: can't find any primitive similar to {file}, using stub. You must set primitive fields manually!");
                var stub = new Primitive(container, file.Name, -1, file.CpuSize, file.GpuSize ?? 0, RfgAsm.Primitive.PrimitiveType.None, RfgAsm.Primitive.PrimitiveAllocator.None, RfgAsm.Primitive.PrimitiveFlags.None, 0);
                container.Primitives.Add(stub);
            }
            else
            {
                if (maxScore != 4)
                {
                    StoreUpdate(container.Id, $"WARNING: found primitives similar to {file}, but not exactly. Check result for correctness!");
                }

                if (ps.Count > 1)
                {
                    StoreUpdate(container.Id, $"WARNING: found {ps.Count} primitives similar to {file}, using first. Check result for correctness!");
                }

                var top = ps.First();
                StoreUpdate(container.Id, $"Found {ps.Count} primitives similar to {file}: score {maxScore}, top {top}");
                var x = new Primitive(container, file.Name, -1, file.CpuSize, file.GpuSize ?? 0, top.Type, top.Allocator, top.Flags, top.Split);
                container.Primitives.Add(x);
            }
        }
    }

    private static readonly string FakeStr2Root = "_ROOT.str2_pc";

    private void SaveResults(string updateVppName, IEnumerable<Container> containers, IEnumerable<Str> strs, CancellationToken token)
    {
        foreach (var dbPrimitive in containers.SelectMany(x => x.Primitives).Select(x => new DbPrimitive(updateVppName, x)))
        {
            token.ThrowIfCancellationRequested();
            archiverState.PrimitiveIndex.Add(dbPrimitive);
        }

        foreach (var f in strs.SelectMany(x => x.Files).Select(x => new DbFile(updateVppName, x)))
        {
            token.ThrowIfCancellationRequested();
            archiverState.FileIndex.Add(f);
        }
    }

    private IEnumerable<Str> IndexStrs(IFileSystemInfo current, IDirectoryInfo rfgDataDir, IEnumerable<string> vppForUpdate, CancellationToken token)
    {
        var vppByName = rfgDataDir
            .EnumerateFiles()
            .Cast<IFileSystemInfo>()
            .Where(x => x.Name.EndsWith(VppDescriptor.Extension, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => FormatDescriptors.RegularFile.ReadEntryForEncoding(x).Name);
        var currentName = FormatDescriptors.RegularFile.ReadEntryForEncoding(current).Name;
        vppByName[currentName] = current;
        var vppReader = new VppReader(OptimizeFor.memory);
        foreach (var vppName in vppForUpdate)
        {
            token.ThrowIfCancellationRequested();
            var dirty = vppName == currentName;
            var vpp = vppByName[vppName] switch
            {
                IDirectoryInfo d => ReadDirectoryAsVpp(d),
                IFileInfo f => vppReader.ReadFile(f, token),
                _ => throw new ArgumentOutOfRangeException()
            };

            var logicalFiles = vpp.LogicalFiles.OrderBy(x => x.Order).ToList();
            var fakeFileOrder = 0;
            var fakeParent = new Str(vpp.Name, FakeStr2Root, -1, -1, []);
            foreach (var logicalFile in logicalFiles)
            {
                if (logicalFile.Name.EndsWith(Str2Descriptor.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    var str = vppReader.Read(logicalFile.Content, logicalFile.Name, token);
                    var strFiles = str.LogicalFiles.OrderBy(x => x.Order).ToList();
                    var parent = new Str(vpp.Name, str.Name, str.DataOffset, str.CompressedSize, []);
                    var fakeStrOrder = 0;
                    foreach (var strFile in strFiles)
                    {
                        ReadFile(vpp.Name, strFiles, strFile, dirty, parent, ref fakeStrOrder);
                    }

                    yield return parent;

                }
                else
                {
                    ReadFile(vpp.Name, logicalFiles, logicalFile, dirty, fakeParent, ref fakeFileOrder);
                }
            }

            yield return fakeParent;
        }
    }

    private LogicalArchive ReadDirectoryAsVpp(IDirectoryInfo directory)
    {
        var info = FormatDescriptors.RegularFile.ReadEntryForEncoding(directory);
        if (info.Properties.VppMode is null)
        {
            throw new ArgumentException($"Directory [{directory.FullName}] has no vppMode in name and can't be properly packed");
        }
        var entries = directory.EnumerateFiles().Select(x =>
        {
            var fileInfo = FormatDescriptors.RegularFile.ReadEntryForEncoding(x);
            if (fileInfo.Properties.Index is null)
            {
                throw new ArgumentException($"File [{x.FullName}] has no index in name and can't be properly packed");
            }

            return new LogicalFile(x.OpenRead(), fileInfo.Name, fileInfo.Properties.Index!, $"fake LogicalFile from real file [{x.FullName}]", null);
        });
        return new LogicalArchive(entries, info.Properties.VppMode.Value, info.Name, -1, -1);
    }

    private void ReadFile(string vppName, IReadOnlyList<LogicalFile> logicalFiles, LogicalFile logicalFile, bool dirty, Str parent, ref int fakeOrder)
    {
        if (AsmHelper.IsIgnoredFile(vppName, logicalFile.Name))
        {
            return;
        }
        var gpu = LocateSecondary(logicalFile, logicalFiles);
        if (logicalFile == gpu)
        {
            return;
        }

        var file = new File(parent, logicalFile.Name, logicalFile.Order, logicalFile.Content.Length, gpu?.Content.Length, dirty, fakeOrder++);
        parent.Files.Add(file);
    }

    /// <summary>
    /// If paired format, for cpu file, return gpu file. Return self if gpu file
    /// </summary>
    private LogicalFile? LocateSecondary(LogicalFile logicalFile, IReadOnlyList<LogicalFile> entries)
    {
        var cpu = PairedFiles.GetCpuFileName(logicalFile.Name);
        var gpu = PairedFiles.GetGpuFileName(logicalFile.Name);
        if (logicalFile.Name.Equals(gpu, StringComparison.OrdinalIgnoreCase))
        {
            return logicalFile;
        }
        var gpuEntry = entries.Skip(logicalFile.Order).FirstOrDefault(x => x.Name == gpu);
        if (string.Equals(logicalFile.Name, cpu, StringComparison.CurrentCultureIgnoreCase) && gpuEntry != null)
        {
            if (gpuEntry.Order != logicalFile.Order + 1)
            {
                throw new InvalidOperationException($"Expected GPU entry to be directly after CPU entry at index [{logicalFile.Order+1}], but found at [{gpuEntry.Order}]");
            }

            return gpuEntry;
        }

        return null;
    }

    private IEnumerable<Container> IndexContainers(IReadOnlyList<VppAndAsm> asmForUpdate)
    {
        foreach (var vpp in asmForUpdate)
        {
            foreach (var asm in vpp.Asm)
            {
                var i = 0;
                foreach (var c in asm.Containers)
                {
                    var container = new Container(vpp.Vpp, asm.Name, c.Name, i, c.DataOffset, c.CompressedSize, c.IsVirtual, [], c.Type, c.Flags);
                    var j = 0;
                    foreach (var p in c.Primitives)
                    {
                        var primitive = new Primitive(container, p.Name, j, p.CpuSize, p.GpuSize, p.Type, p.Allocator, p.Flags, p.Split);
                        container.Primitives.Add(primitive);
                        j++;
                    }

                    yield return container;
                    i++;
                }
            }
        }
    }

    /// <summary>
    /// Given a modified VPP, returns VPPS with ASMs that need updating
    /// and all VPPs required to reference all primitives
    /// </summary>
    /// <param name="current">asm files from current vpp</param>
    /// <param name="all">asm files from all vpps in data directory</param>
    private IEnumerable<VppAndAsm> GetVppAndAsmForUpdate(VppAndAsm current, IReadOnlyList<VppAndAsm> all)
    {
        // always update self
        yield return current;
        var vppByName = all.ToDictionary(x => x.Vpp, StringComparer.OrdinalIgnoreCase);
        // add extra asm files in known cross-reference cases
        switch (current.Vpp.ToLowerInvariant())
        {
            case "terr01_l0.vpp_pc":
                yield return vppByName["terr01_l1.vpp_pc"];
                yield return vppByName["terr01_precache.vpp_pc"];
                yield return vppByName["effects.vpp_pc"];
                yield return vppByName["dlc01_precache.vpp_pc"];
                yield break;
            case "terr01_l1.vpp_pc":
                yield return vppByName["terr01_l0.vpp_pc"];
                yield return vppByName["terr01_precache.vpp_pc"];
                yield return vppByName["effects.vpp_pc"];
                yield return vppByName["dlc01_precache.vpp_pc"];
                yield break;
            case "terr01_precache.vpp_pc":
                yield return vppByName["terr01_l0.vpp_pc"];
                yield return vppByName["terr01_l1.vpp_pc"];
                yield return vppByName["effects.vpp_pc"];
                yield return vppByName["dlc01_precache.vpp_pc"];
                yield break;
            case "effects.vpp_pc":
                yield return vppByName["terr01_l0.vpp_pc"];
                yield return vppByName["terr01_l1.vpp_pc"];
                yield return vppByName["terr01_precache.vpp_pc"];
                yield return vppByName["dlc01_precache.vpp_pc"];
                yield break;
            case "dlc01_precache.vpp_pc":
                yield return vppByName["terr01_l0.vpp_pc"];
                yield return vppByName["terr01_l1.vpp_pc"];
                yield return vppByName["terr01_precache.vpp_pc"];
                yield return vppByName["effects.vpp_pc"];
                yield return vppByName["dlc01_l0.vpp_pc"];
                yield break;
            case "dlc01_l0.vpp_pc":
                yield return vppByName["dlc01_precache.vpp_pc"];
                yield break;
            case "mp_common.vpp_pc":
                foreach (var x in all.Where(x => AsmHelper.IsMultiplayerVpp(x.Vpp)))
                {
                    if (!x.Vpp.Equals("mp_common.vpp_pc", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return x;
                    }
                }

                yield break;
            case {} x when AsmHelper.IsMultiplayerVpp(x):
                yield return vppByName["mp_common.vpp_pc"];
                yield break;
        }
    }

    private record VppAndAsm(string Vpp, IReadOnlyList<LogicalAsm> Asm)
    {
        public override string ToString() => $"{Vpp} with {string.Join(", ", Asm.Select(x => x.Name))}";
    }

    private async Task<IReadOnlyList<VppAndAsm>> ReadAllAsmFromAllVpp(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        List<VppAndAsm> result = new();
        foreach (var file in rfgDataDir.EnumerateFiles().Where(x => x.Extension.Equals(".vpp_pc", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
        {
            token.ThrowIfCancellationRequested();
            var vppReader = new VppReader(OptimizeFor.memory);
            var vpp = vppReader.ReadFile(file, token);
            var asmEntries = vpp.LogicalFiles.Where(x => x.Name.EndsWith(AsmDescriptor.Extension)).OrderBy(x => x.Name).ToList();
            var items = asmEntries.Select(x => asmReader.Read(x.Content, x.Name, token)).ToList();
            result.Add(new VppAndAsm(vpp.Name, items));
        }

        return result;
    }

    private VppAndAsm ReadAllAsmFromVpp(IFileSystemInfo vpp, CancellationToken token)
    {
        var vppName = FormatDescriptors.RegularFile.ReadEntryForEncoding(vpp).Name;
        var asms = vpp switch
        {
            IFileInfo f => ReadAllAsmFromFile(f, token),
            IDirectoryInfo d => ReadAllAsmFromDirectory(d, token),
        };
        return new VppAndAsm(vppName, asms.ToList());
    }

    private IEnumerable<LogicalAsm> ReadAllAsmFromFile(IFileInfo file, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var vppReader = new VppReader(OptimizeFor.memory);
        var vpp = vppReader.ReadFile(file, token);
        var asmEntries = vpp.LogicalFiles.Where(x => x.Name.EndsWith(AsmDescriptor.Extension)).OrderBy(x => x.Name);
        return asmEntries.Select(x => asmReader.Read(x.Content, x.Name, token));
    }

    private IEnumerable<LogicalAsm> ReadAllAsmFromDirectory(IDirectoryInfo directory, CancellationToken token)
    {
        var files = directory.EnumerateFiles().Where(x => x.Name.EndsWith(AsmDescriptor.JsonExtension, StringComparison.OrdinalIgnoreCase));
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            using var f = file.OpenRead();
            yield return asmReader.ReadJson(f, file.Name);
        }
    }
}

public record File(Str Parent, string Name, long Order, long CpuSize, long? GpuSize, bool Dirty, long FakeOrder)
{
    public string Id { get; set; } = $"{Parent.Id}/{Name}";
}

public record Str(string Vpp, string Name, long Offset, long CompressedSize, List<File> Files)
{
    public int Count => Files.Count;
    public string Id { get; set; } = $"{Vpp}/{Name}";
}

public record Container(string Vpp, string Asm, string Name, long Index, long Offset, long CompressedSize, bool IsVirtual, List<Primitive> Primitives, RfgAsm.Container.ContainerType Type, RfgAsm.Container.ContainerFlags Flags)
{
    public int Count => Primitives.Count;
    public string Id { get; set; } = $"{Vpp}/{Asm}/{Name}";
}

public record Primitive(Container Parent, string Name, long Order, long CpuSize, long GpuSize, RfgAsm.Primitive.PrimitiveType Type, RfgAsm.Primitive.PrimitiveAllocator Allocator, RfgAsm.Primitive.PrimitiveFlags Flags, byte Split)
{
    public string Id { get; set; } = $"{Parent.Id}/{Name}";

}

public class DbPrimitive(string updateFrom, Primitive p)
{
    public DbPrimitive() : this(null, null){}

    public string UpdateFrom { get; init; } = updateFrom.ToLowerInvariant();
    public string Vpp { get; init; } = p.Parent.Vpp.ToLowerInvariant();
    public string Asm { get; init; } = p.Parent.Asm.ToLowerInvariant();
    public string Container { get; init; } = p.Parent.Name.ToLowerInvariant();
    public long ContainerIndex { get; init; } = p.Parent.Index;
    public long ContainerOffset { get; init; } = p.Parent.Offset;
    public long ContainerCompressedSize { get; init; } = p.Parent.CompressedSize;
    public bool IsVirtual { get; init; } = p.Parent.IsVirtual;
    public string Name { get; init; } = p.Name.ToLowerInvariant();
    public long Order { get; init; } = p.Order;
    public long CpuSize { get; init; } = p.CpuSize;
    public long GpuSize { get; init; } = p.GpuSize;
    public string Id { get; init; } = $"{updateFrom}#{p.Parent.Vpp}/{p.Parent.Asm}/{p.Parent.Name}/{p.Name}";
}

public class DbFile(string updateFrom, File f)
{
    public DbFile() : this(null, null){}

    public string UpdateFrom { get; init; } = updateFrom.ToLowerInvariant();
    public string Vpp { get; init; } = f.Parent.Vpp.ToLowerInvariant();
    public string Name { get; init; } = f.Name.ToLowerInvariant();
    public long Order { get; init; } = f.Order;
    public long FakeOrder { get; init; } = f.FakeOrder;
    public long CpuSize { get; init; } = f.CpuSize;
    public long GpuSize { get; init; } = f.GpuSize ?? -1;
    public bool Dirty { get; init; } = f.Dirty;
    public string Str { get; init; } = f.Parent.Name.ToLowerInvariant();
    public long StrOffset { get; init; } = f.Parent.Offset;
    public long StrCompressedSize { get; init; } = f.Parent.CompressedSize;
    public string Id { get; set; } = $"{updateFrom}#{f.Parent.Vpp}/{f.Parent.Name}/{f.Name}";
}

public class DbAsmUpdate
{
    public int Id { get; set; }
    public string Vpp { get; set; }
    public string ItemId { get; set; }
    public string Message { get; set; }
}
