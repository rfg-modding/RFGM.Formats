using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Db;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;
using RFGM.Formats.Abstractions.Descriptors;
using RFGM.Formats.Asm;
using RFGM.Formats.Asm.Models;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Archiver.Services;

public class AsmFileLocator(AsmReader asmReader, ILogger<AsmFileLocator> log)
{
    public async Task<IReadOnlyList<LocateMessage>> InitMessages(IDirectoryInfo rfgDataDir, CancellationToken token)
    {
        List<NestedItem<LogicalAsm>> asms = new();
        foreach (var file in rfgDataDir.EnumerateFiles().Where(x => x.Extension.Equals(".vpp_pc", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
        {
            token.ThrowIfCancellationRequested();
            await using var f = file.OpenRead();
            var vppReader = new VppReader(OptimizeFor.memory);
            var vpp = vppReader.ReadFile(file, token);
            var asmEntries = vpp.LogicalFiles.Where(x => x.Name.EndsWith(AsmDescriptor.Extension)).OrderBy(x => x.Name).ToList();
            var items = asmEntries.Select(x => asmReader.Read(x.Content, x.Name, token)).Select(x => new NestedItem<LogicalAsm>(Breadcrumbs.Init().Descend(vpp.Name), x));
            asms.AddRange(items);
        }

        var result = new List<LocateMessage>();
        foreach (var x in asms)
        {
            token.ThrowIfCancellationRequested();
            var vpp = x.Breadcrumbs.ToString();
            foreach (var container in x.Item.Containers)
            {
                token.ThrowIfCancellationRequested();
                var containerBreadcrumbs = x.Breadcrumbs.Descend(x.Item.Name).Descend(container.Name);
                foreach (var primitive in container.Primitives)
                {
                    token.ThrowIfCancellationRequested();
                    var l = new LocateMessage(container, primitive, vpp, x.Item.Name, containerBreadcrumbs, rfgDataDir);
                    result.Add(l);
                }
            }
        }

        return result;
    }

    public async Task<PrimitiveLocation> LocateFileForPrimitive(LocateMessage message, CancellationToken token)
    {
        var (container, primitive, vppName, asmName, containerBreadcrumbs, rfgDataDir) = message;
        var primitiveId = containerBreadcrumbs.Descend(primitive.Name).ToString();
        if (AsmHelper.MissingVanillaPrimitives.Contains(primitiveId))
        {
            return new PrimitiveLocation
            {
                Id = primitiveId,
                File = null,
                Status = "missing",
                Message = $"This is fine, vanilla game references some missing files"
            };
        }

        List<string> vppLookup = VppLookup(container.IsVirtual, vppName, asmName);
        var reader = new VppReader(OptimizeFor.memory);
        foreach (var vpp in vppLookup)
        {
            token.ThrowIfCancellationRequested();
            var file = rfgDataDir.EnumerateFiles().Single(x => x.Name.Equals(vpp, StringComparison.OrdinalIgnoreCase));
            var archive = reader.ReadFile(file, token);
            var files = ListFilesFromSuitablePlaces(archive, container, asmName, token);
            var name = AsmHelper.FixableVanillaNames.GetValueOrDefault(primitive.Name, primitive.Name);
            var candidates = files.Where(x => x.Item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (candidates.Count == 1)
            {
                return Check(candidates.Single(), primitive, primitiveId, archive, "Single candidate", token);
            }

            if (candidates.Count > 1)
            {
                if (primitive.Name.EndsWith(".rfgzone_pc", StringComparison.OrdinalIgnoreCase))
                {
                    // mp maps have duplicate in vpp and ns_base.str
                    var c = candidates.Single(x => !x.Breadcrumbs.ToString().Contains(".str2_pc", StringComparison.OrdinalIgnoreCase));
                    return Check(c, primitive, primitiveId, archive, "Selected from multiple candidates: rfgzone in MP VPP", token);
                }

                var candidateStr = string.Join(", ", candidates.Select(x => x.Breadcrumbs.Descend(x.Item.Name).ToString()));
                var msg = $"Multiple candidates: {candidateStr}, current vpp {vpp} from lookup {string.Join(",", vppLookup)}";
                var checks = candidates.Select(x => Check(x, primitive, primitiveId, archive, msg, token)).ToList();
                var good = checks.Where(x => x.Status == "ok").ToList();

                if (good.Count == 1)
                {
                    var x = good.Single();
                    return new PrimitiveLocation
                    {
                        Id = x.Id,
                        File = x.File,
                        Status = x.Status,
                        Message = $"{x.Message} / Selected single good match"
                    };
                }

                if (good.Count > 1)
                {
                    var x = good.First();
                    return new PrimitiveLocation
                    {
                        Id = x.Id,
                        File = x.File,
                        Status = x.Status,
                        Message = $"{x.Message} / Selected first good match"
                    };
                }

                // TODO is this case important or let it fall to next iteration?
                /*if (good.Count == 0)
                {
                    var x = checks.First();
                    return new PrimitiveLocation()
                    {
                        Id = x.Id,
                        File = x.File,
                        Status = x.Status,
                        Message = $"{x.Message} / No good match"
                    };
                }*/
            }
        }

        return new PrimitiveLocation
        {
            Id = primitiveId,
            Status = "zeroCandidates",
            File = null,
            Message = $"Zero candidates, reached end of lookup {string.Join(",", vppLookup)}"
        };
    }

    private IEnumerable<NestedItem<LogicalFile>> ListFilesFromSuitablePlaces(LogicalArchive archive, LogicalContainer container, string asmName, CancellationToken token)
    {
        // mp_common and *precache only have files without containers
        var breadcrumbs = Breadcrumbs.Init().Descend(archive.Name);
        if (AsmHelper.PlainVpps.Contains(archive.Name))
        {
            return archive.LogicalFiles.Select(x => new NestedItem<LogicalFile>(breadcrumbs, x));
        }

        var allStr = archive.LogicalFiles
                .Where(x => x.Name.EndsWith(".str2_pc", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Name);

        // for mp/wc maps, there are only 2 strs + files without containers
        if (AsmHelper.IsMultiplayerVpp(archive.Name))
        {
            return allStr.Values
                .SelectMany(x => ReadStr(x, token).Select(y => new NestedItem<LogicalFile>(breadcrumbs.Descend(x.Name), y)))
                .Concat(archive.LogicalFiles.Select(x => new NestedItem<LogicalFile>(breadcrumbs, x)));
        }

        // look everywhere because primitives from these containers are all over the place
        var lookEverywhere = container.Name.EndsWith(".rfgchunkx", StringComparison.OrdinalIgnoreCase)
                         || container.Name.EndsWith(".rfgstitchx", StringComparison.OrdinalIgnoreCase)
                         || (container.IsVirtual && asmName.Equals("vfx_containers.asm_pc", StringComparison.OrdinalIgnoreCase));
        if (lookEverywhere)
        {
            return allStr.Values
                .SelectMany(x => ReadStr(x, token).Select(y => new NestedItem<LogicalFile>(breadcrumbs.Descend(x.Name), y)));
        }

        // rename certain containers to match by known rules
        return GetContainerAltNames(container.Name)
            .Select(x => allStr.GetValueOrDefault(x))
            .Where(x => x != null)
            .Cast<LogicalFile>()
            .SelectMany(x => ReadStr(x, token).Select(y => new NestedItem<LogicalFile>(breadcrumbs.Descend(x.Name), y)));
    }

    private IEnumerable<LogicalFile> ReadStr(LogicalFile str, CancellationToken token)
    {
        var reader = new VppReader(OptimizeFor.memory);
        var archive = reader.Read(str.Content.MakeDeepOwnCopy(), str.Name, token);
        return archive.LogicalFiles;
    }

    private static List<string> VppLookup(bool isVirtual, string vpp, string asm)
    {
        var path = $"{vpp}/{asm}".ToLowerInvariant();
        return path switch
        {
            // normal container
            _ when !isVirtual => [vpp],
            // virtual container lookup
            "terr01_l0.vpp_pc/terr01_l0.asm_pc" => ["terr01_precache.vpp_pc", "terr01_l0.vpp_pc", "terr01_l1.vpp_pc"], // TODO order: is precache more important than l0/l1?
            "effects.vpp_pc/vfx_containers.asm_pc" => ["effects.vpp_pc", "terr01_precache.vpp_pc", "terr01_l0.vpp_pc", "terr01_l1.vpp_pc"],
            "dlc01_l0.vpp_pc/dlc01_l0.asm_pc" => ["dlc01_precache.vpp_pc", "dlc01_l0.vpp_pc"],
            // standalone map or mp_common
            _ when AsmHelper.IsMultiplayerVpp(vpp) => ["mp_common.vpp_pc", vpp],
            // unknown virtual container
            _ => throw new ArgumentOutOfRangeException($"{path}, isVirtual={isVirtual}")
        };
    }

    private PrimitiveLocation Check(NestedItem<LogicalFile> item, LogicalPrimitive primitive, string primitiveId, LogicalArchive archive, string message, CancellationToken token)
    {
        var path = item.Breadcrumbs.ToList();
        var entriesByIndex = GetEntries(archive, path, token);
        var cpu = item.Item;
        // some .cefct_pc don't have corresponding gpu file
        var cpuName = AsmHelper.FixableVanillaNames.GetValueOrDefault(primitive.Name, primitive.Name);
        var gpuName = primitive.GpuSize > 0 ? PairedFiles.GetGpuFileName(cpuName) : null;
        var gpu = gpuName != null ? entriesByIndex[cpu.Order + 1] : null;
        var checkError = Сheck(cpu, gpu, primitive, cpuName, gpuName);
        if (checkError != null)
        {
            return new PrimitiveLocation
            {
                Id = primitiveId,
                Status = "checkError",
                File = item.Breadcrumbs.Descend(item.Item.Name).ToString(),
                Message = $"{message} / {checkError}"
            };
        }
        return new PrimitiveLocation
        {
            Id = primitiveId,
            Status = "ok",
            File = item.Breadcrumbs.Descend(item.Item.Name).ToString(),
            Message = message
        };
    }

    private string? Сheck(LogicalFile cpu, LogicalFile? gpu, LogicalPrimitive primitive, string cpuName, string? gpuName)
    {
        // TODO can't check container here: data offset, compressed length, primitive order...

        if (!cpu.Name.Equals(cpuName, StringComparison.OrdinalIgnoreCase))
        {
            return $"Invalid cpu name [{cpu.Name}]";
        }

        if (gpu != null && !gpu.Name.Equals(gpuName, StringComparison.OrdinalIgnoreCase))
        {
            return $"Invalid gpu name [{gpu.Name}]";
        }

        if (cpu.Content.Length != primitive.CpuSize)
        {
            return $"Invalid cpu size [{cpu.Content.Length}]";
        }

        if ((gpu?.Content.Length ?? 0) != primitive.GpuSize)
        {
            return $"Invalid gpu size [{gpu?.Content.Length}]";
        }

        return null;
    }

    private IReadOnlyList<LogicalFile> GetEntries(LogicalArchive archive, IReadOnlyList<string> path, CancellationToken token)
    {
        if (path.Count != 1 && path.Count != 2)
        {
            throw new ArgumentException($"Expected path depth 1 or 2, got {path.Count} [{string.Join("/", path)}]");
        }

        if (path.First() != archive.Name)
        {
            throw new ArgumentException($"Expected path in archive [{archive.Name}], got [{string.Join("/", path)}]");
        }

        var str = path.Count == 2 ? path.Last() : null;
        if (str is null)
        {
            return archive.LogicalFiles.ToList();
        }

        var entry = archive.LogicalFiles.Single(x => x.Name.Equals(str, StringComparison.OrdinalIgnoreCase));
        var vppReader = new VppReader(OptimizeFor.memory);
        var container = vppReader.Read(entry.Content.MakeDeepOwnCopy(), entry.Name, token);
        return container.LogicalFiles.ToList();
    }

    private IEnumerable<string> GetContainerAltNames(string containerName)
    {
        yield return $"{containerName}.str2_pc";

        var match = FixableContainerName.Match(containerName);
        if (match.Success)
        {
            // "1_c0608" => "1h_c0608.str2_pc"
            yield return $"{match.Groups["number"]}h_{match.Groups["id"]}.str2_pc";
        }
    }

    private static readonly Regex FixableContainerName = new(@"^(?<number>\d+)_(?<id>c\d+)$", RegexOptions.Compiled);

}
