using System.Collections.Immutable;
using System.IO.Abstractions;
using RFGM.Formats;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Models;

public record UnpackMessage(IFileInfo Source, string FileGlob, IReadOnlyList<string> RelativePath, IDirectoryInfo OutputPath, ImmutableHashSet<FileFormat> Formats, ImmutableHashSet<ImageFormat> Textures, bool XmlFormat, bool Recursive, bool Metadata, bool Hash, bool Force) : IMessage
{
    public static UnpackMessage Default(IFileInfo archive, IFileSystem fileSystem) => new UnpackMessage(archive, "*", [], fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(archive.Directory!.FullName, Constants.DefaultDir)), FormatManager.CanRead, [ImageFormat.png], false, false, true, false, true);
}
