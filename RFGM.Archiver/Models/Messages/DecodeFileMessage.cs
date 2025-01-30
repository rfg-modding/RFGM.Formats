using System.IO.Abstractions;
using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record DecodeFileMessage(EntryInfo EntryInfo, Stream Primary, Breadcrumbs Breadcrumbs, IDirectoryInfo Destination, UnpackSettings Settings) : IMessage;
