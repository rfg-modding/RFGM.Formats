
namespace RFGM.Formats.Asm.Models;

public record LogicalPrimitive(
    string Name,
    RfgAsm.Primitive.PrimitiveType Type,
    RfgAsm.Primitive.PrimitiveAllocator Allocator,
    RfgAsm.Primitive.PrimitiveFlags Flags,
    byte Split,
    long CpuSize,
    long GpuSize
);
