using System.IO.Abstractions;
using RFGM.Formats;
using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record PackDirectoryMessage(IDirectoryInfo DirectoryInfo, string Destination, PackSettings Settings) : IMessage;
