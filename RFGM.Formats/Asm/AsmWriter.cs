using RFGM.Formats.Asm.Models;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Asm;

public class AsmWriter(LogicalAsm asm)
{
    public async Task WriteAll(Stream destination, CancellationToken token)
    {
        BinUtils.CheckStream(destination);
        throw new NotImplementedException();
    }

    private IEnumerable<uint> SerializeSizes(List<LogicalPrimitive> primitives)
    {
        foreach (var primitive in primitives)
        {
            yield return (uint)primitive.CpuSize;
            if (primitive.GpuSize != 0)
            {
                yield return (uint)primitive.GpuSize;
            }
        }
    }
}
