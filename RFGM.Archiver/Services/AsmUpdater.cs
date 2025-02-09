using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Formats;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Abstractions.Descriptors;
using RFGM.Formats.Asm;
using RFGM.Formats.Asm.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class AsmUpdater(AsmReader asmReader, ArchiverState archiverState, ILogger<AsmUpdater> log)
{
    public async Task Update(IFileSystemInfo vpp, IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        if (!vpp.Name.Contains("mp_cornered"))
        {
            return;
        }

        var vppName = FormatDescriptors.RegularFile.ReadEntryForEncoding(vpp).Name;
        var current = ReadAllAsmFromVpp(vpp, token);
        var all = await ReadAllAsmFromAllVpp(rfgDataDir, token);
        var forUpdate = GetVppAndAsmForUpdate(current, all).ToList();
        log.LogInformation("Vpp [{}], found {} asm files to update:\n\t{}", vpp.Name, forUpdate.Count, string.Join("\n\t", forUpdate));

        var primitives = IndexPrimitives(forUpdate);
        var files = IndexFiles(vpp, rfgDataDir, forUpdate.Select(x => x.Vpp), token);
        SaveResults(vppName, primitives, files, token);


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

    private void SaveResults(string updateVppName, IEnumerable<Primitive> primitives, IEnumerable<IFile> files, CancellationToken token)
    {
        foreach (var dbPrimitive in primitives.Select(x => new DbPrimitive(updateVppName, x)))
        {
            token.ThrowIfCancellationRequested();
            archiverState.PrimitiveIndex.Add(dbPrimitive);

        }

        foreach (var f in files)
        {
            token.ThrowIfCancellationRequested();
            var d = new DbFile(updateVppName, f);
            archiverState.FileIndex.Add(d);
        }
    }

    private IEnumerable<IFile> IndexFiles(IFileSystemInfo current, IDirectoryInfo rfgDataDir, IEnumerable<string> vppForUpdate, CancellationToken token)
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
            foreach (var logicalFile in logicalFiles)
            {
                if (logicalFile.Name.EndsWith(Str2Descriptor.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    var str = vppReader.Read(logicalFile.Content, logicalFile.Name, token);
                    var strFiles = str.LogicalFiles.OrderBy(x => x.Order).ToList();
                    var fakeStrOrder = 0;
                    foreach (var strFile in strFiles)
                    {
                        var file = ReadStrFile(vpp.Name, str, strFiles, strFile, dirty, ref fakeStrOrder);
                        if (file != null)
                        {
                            yield return file;
                        }
                    }

                }
                else
                {
                    var file = ReadFile(vpp.Name, logicalFiles, logicalFile, dirty, ref fakeFileOrder);
                    if (file != null)
                    {
                        yield return file;
                    }
                }
            }
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

    private StrFile? ReadStrFile(string vppName, LogicalArchive str, IReadOnlyList<LogicalFile> logicalFiles, LogicalFile logicalFile, bool dirty, ref int fakeOrder)
    {
        if (AsmHelper.IsIgnoredFile(vppName, logicalFile.Name))
        {
            return null;
        }
        var gpu = LocateSecondary(logicalFile, logicalFiles);
        if (logicalFile == gpu)
        {
            return null;
        }

        return new StrFile(vppName, logicalFile.Name, logicalFile.Order, logicalFile.Content.Length, gpu?.Content.Length, dirty, fakeOrder++, str.Name, str.DataOffset, str.CompressedSize);
    }

    private File? ReadFile(string vppName, IReadOnlyList<LogicalFile> logicalFiles, LogicalFile logicalFile, bool dirty, ref int fakeOrder)
    {
        if (AsmHelper.IsIgnoredFile(vppName, logicalFile.Name))
        {
            return null;
        }
        var gpu = LocateSecondary(logicalFile, logicalFiles);
        if (logicalFile == gpu)
        {
            return null;
        }

        return new File(vppName, logicalFile.Name, logicalFile.Order, logicalFile.Content.Length, gpu?.Content.Length, dirty, fakeOrder++);
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

    private IEnumerable<Primitive> IndexPrimitives(IReadOnlyList<VppAndAsm> asmForUpdate)
    {
        foreach (var vpp in asmForUpdate)
        {
            foreach (var asm in vpp.Asm)
            {
                var i = 0;
                foreach (var c in asm.Containers)
                {
                    var j = 0;
                    foreach (var p in c.Primitives)
                    {
                        yield return new Primitive(vpp.Vpp, asm.Name, c.Name, i, c.DataOffset, c.CompressedSize, c.IsVirtual, p.Name, j, p.CpuSize, p.GpuSize);
                        j++;
                    }
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

public interface IFile
{
    string Vpp { get; }
    string Name { get; }
    long Order { get; }
    long CpuSize { get; }
    long? GpuSize { get; }
    bool Dirty { get; }
    long FakeOrder { get; }
}

public record File(string Vpp, string Name, long Order, long CpuSize, long? GpuSize, bool Dirty, long FakeOrder) : IFile
{
}

public record StrFile(string Vpp, string Name, long Order, long CpuSize, long? GpuSize, bool Dirty, long FakeOrder, string Str, long StrOffset, long StrCompressedSize) : IFile
{
}

public record Primitive(string Vpp, string Asm, string Container, long ContainerIndex, long ContainerOffset, long ContainerCompressedSize, bool IsVirtual, string Name, long Order, long CpuSize, long GpuSize);

public class DbPrimitive(string updateFrom, Primitive p)
{
    public DbPrimitive() : this(null, null){}

    public string UpdateFrom { get; init; } = updateFrom.ToLowerInvariant();
    public string Vpp { get; init; } = p.Vpp.ToLowerInvariant();
    public string Asm { get; init; } = p.Asm.ToLowerInvariant();
    public string Container { get; init; } = p.Container.ToLowerInvariant();
    public long ContainerIndex { get; init; } = p.ContainerIndex;
    public long ContainerOffset { get; init; } = p.ContainerOffset;
    public long ContainerCompressedSize { get; init; } = p.ContainerCompressedSize;
    public bool IsVirtual { get; init; } = p.IsVirtual;
    public string Name { get; init; } = p.Name.ToLowerInvariant();
    public long Order { get; init; } = p.Order;
    public long CpuSize { get; init; } = p.CpuSize;
    public long GpuSize { get; init; } = p.GpuSize;
    public string Id { get; init; } = $"{updateFrom}#{p.Vpp}/{p.Asm}/{p.Container}/{p.Name}";


}

public class DbFile(string updateFrom, IFile f)
{
    public DbFile() : this(null, null){}

    public string UpdateFrom { get; init; } = updateFrom.ToLowerInvariant();
    public string Vpp { get; init; } = f.Vpp.ToLowerInvariant();
    public string Name { get; init; } = f.Name.ToLowerInvariant();
    public long Order { get; init; } = f.Order;
    public long FakeOrder { get; init; } = f.FakeOrder;
    public long CpuSize { get; init; } = f.CpuSize;
    public long GpuSize { get; init; } = f.GpuSize ?? -1;
    public bool Dirty { get; init; } = f.Dirty;
    public string Str { get; init; } = (f as StrFile)?.Str.ToLowerInvariant() ?? "_PLAIN";
    public long StrOffset { get; init; } = (f as StrFile)?.StrOffset ?? -1;
    public long StrCompressedSize { get; init; } = (f as StrFile)?.StrCompressedSize ?? -1;

    public string Id { get; set; } = GetId(updateFrom, f);

    private static string GetId(string updateFrom, IFile f)
    {
        var strPart = f is StrFile s ? $"{s.Str}/" : "";
        return $"{updateFrom}#{f.Vpp}/{strPart}{f.Name}";
    }
}
