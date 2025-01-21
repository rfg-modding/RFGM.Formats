using System.Collections.Immutable;
using System.IO.Abstractions;
using RFGM.Archiver.Models.Metadata;
using RFGM.Archiver.Services;
using RFGM.Formats;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Models.Messages;

public record UnpackMessage(IFileInfo Source, string FileGlob, Breadcrumbs Breadcrumbs, IDirectoryInfo OutputPath, ImmutableHashSet<FileFormat> Formats, ImmutableHashSet<ImageFormat> Textures, bool XmlFormat, bool Recursive, bool Metadata, bool Hash, bool Force) : IMessage
{
    public static UnpackMessage Default(IFileInfo archive, IFileSystem fileSystem) => new UnpackMessage(archive, "*", Breadcrumbs.Init(), fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(archive.Directory!.FullName, Constants.DefaultDir)), FormatManager.CanRead, [ImageFormat.png], false, false, true, false, true);
}
