using System.Text.Json;
using RFGM.Formats.Asm.Models;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Asm;

public class AsmWriter(LogicalAsm asm)
{
    public async Task Write(Stream destination, CancellationToken token)
    {
        BinUtils.CheckStream(destination);
        throw new NotImplementedException();
    }

    public async Task WriteJson(Stream destination, CancellationToken token)
    {
        BinUtils.CheckStream(destination);
        await JsonSerializer.SerializeAsync(destination, asm, FormatUtils.UserFriendlyJsonOptions, token);
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
