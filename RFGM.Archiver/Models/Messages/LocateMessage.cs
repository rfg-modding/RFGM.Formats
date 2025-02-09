using System.IO.Abstractions;
using RFGM.Formats.Asm.Models;

namespace RFGM.Archiver.Models.Messages;

public record LocateMessage(LogicalContainer Container, LogicalPrimitive Primitive, string VppName, string AsmName, Breadcrumbs ContainerBreadcrumbs, IDirectoryInfo RfgDataDir) : IMessage;
