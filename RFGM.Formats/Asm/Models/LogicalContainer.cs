namespace RFGM.Formats.Asm.Models;

public record LogicalContainer(
    string Name,
    RfgAsm.Container.ContainerType Type,
    RfgAsm.Container.ContainerFlags Flags,
    long DataOffset,
    long CompressedSize,
    bool IsVirtual,
    IReadOnlyList<LogicalPrimitive> Primitives
);
