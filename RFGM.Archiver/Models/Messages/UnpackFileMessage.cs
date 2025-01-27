using System.IO.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record UnpackFileMessage(IFileInfo FileInfo, string Destination, UnpackSettings Settings) : IMessage;