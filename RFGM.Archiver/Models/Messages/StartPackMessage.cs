using System.IO.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record StartPackMessage(IFileSystemInfo FileSystemInfo, string Destination, PackSettings Settings) : IMessage;
