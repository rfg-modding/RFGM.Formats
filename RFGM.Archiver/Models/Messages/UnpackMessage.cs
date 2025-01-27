using System.IO.Abstractions;
using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Models.Messages;

public record UnpackMessage(EntryInfo EntryInfo, Stream Primary, Stream? Secondary, Breadcrumbs Breadcrumbs, IDirectoryInfo Destination, UnpackSettings Settings) : IMessage;
