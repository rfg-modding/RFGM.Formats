using System.IO.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record UpdateAsmMessage(IFileSystemInfo Target, IDirectoryInfo RfgDataDir) : IMessage;
