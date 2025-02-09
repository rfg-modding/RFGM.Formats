using System.Text.Json;
using Kaitai;
using Microsoft.Extensions.Logging;
using RFGM.Formats.Asm.Models;

namespace RFGM.Formats.Asm;

public class AsmReader(ILogger<AsmReader> log)
{
    public LogicalAsm ReadJson(Stream input, string name)
    {
        var containers = JsonSerializer.Deserialize<List<LogicalContainer>>(input, FormatUtils.JsonOptions);
        ArgumentNullException.ThrowIfNull(containers);
        return new LogicalAsm(containers, name);
    }

    public LogicalAsm Read(Stream input, string name, CancellationToken token)
    {
        var s = new KaitaiStream(input);
        var asm = new RfgAsm(s);
        var containers = LogicalContainers(asm, token).ToList();
        return new LogicalAsm(containers, name);
    }

    private IEnumerable<LogicalContainer> LogicalContainers(RfgAsm asm, CancellationToken token)
    {
        foreach (var x in asm.Containers)
        {
            token.ThrowIfCancellationRequested();
            var isVirtual = x.Sizes.Count == 0 && x.Primitives.Count > 0;
            var primitives = LogicalPrimitives(x, isVirtual, token).ToList();
            yield return new LogicalContainer(x.Name, x.Type, x.FlagsEnum, x.DataOffset, x.LenCompressedData, isVirtual, primitives);
        }
    }

    private IEnumerable<LogicalPrimitive> LogicalPrimitives(RfgAsm.Container container, bool isVirtual, CancellationToken token)
    {
        var i = 0;
        uint ReadSize() => container.Sizes[i++];

        foreach (var x in container.Primitives)
        {
            //log.LogDebug("Reading: container [{container}], primitive [{primitive}], size index [{i}]", container.Name, x.Name, i);
            token.ThrowIfCancellationRequested();
            if (x.LenCpu != 0 && !isVirtual)
            {
                var size = ReadSize();
                if (size != x.LenCpu)
                {
                    throw new InvalidOperationException($"Expected header size [{x.LenCpu}], got [{size}] for primitive [{x.Name}] in container [{container.Name}]");
                }
            }

            if (x.LenGpu != 0 && !isVirtual)
            {
                var size = ReadSize();
                if (size != x.LenGpu)
                {
                    throw new InvalidOperationException($"Expected data size [{x.LenGpu}], got [{size}] for primitive [{x.Name}] in container [{container.Name}]");
                }
            }

            yield return new LogicalPrimitive(x.Name, x.Type, x.Allocator, x.FlagsEnum, x.SplitExtIndex, x.LenCpu, x.LenGpu);
        }

        if (i != container.Sizes.Count)
        {
            throw new InvalidOperationException($"Expected to read all [{container.Sizes.Count}] sizes, but read [{i}] in container [{container.Name}]");
        }
    }
}
