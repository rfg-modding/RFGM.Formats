using System.Text;

namespace RFGM.Formats.Asm.Models;

public record LogicalAsm(
    IReadOnlyList<LogicalContainer> Containers,
    string Name
)
{
    public string ToCsv()
    {
        var sb = new StringBuilder();
        foreach (var c in Containers)
        {
            foreach (var p in c.Primitives)
            {
                var cFlags = c.Flags.ToString().Replace(", ", "+");
                var pFlags = p.Flags.ToString().Replace(", ", "+");
                var cName = c.Name.Replace(",", "*");
                var pName = p.Name.Replace(",", "*");
                sb.AppendLine($"{Name},{cName},{c.Type},{cFlags},{c.DataOffset},{pName},{p.Type},{pFlags},{p.Allocator},{p.CpuSize},{p.GpuSize}");
            }
        }

        return sb.ToString();
    }

    public static string ToCsvHeader() => $"asmName,cName,cType,cFlags,cDOffs,prName,prType,prFlags,prAllocator,prHLen,prHSz,prDLen,prDSz";
}
