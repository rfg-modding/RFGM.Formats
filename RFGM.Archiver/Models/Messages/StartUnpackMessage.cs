using System.IO.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record StartUnpackMessage(IFileInfo FileInfo, string Destination, UnpackSettings Settings) : IMessage;
