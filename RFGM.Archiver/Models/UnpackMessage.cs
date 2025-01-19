using System.Collections.Immutable;
using System.IO.Abstractions;
using RFGM.Formats;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Models;

public record UnpackMessage(IFileInfo Archive, string FileGlob, IReadOnlyList<string> RelativePath, IDirectoryInfo OutputPath, bool XmlFormat, bool Recursive, ImmutableHashSet<FileFormat> Formats, ImmutableHashSet<ImageFormat> Textures, bool Metadata, bool Force, bool Hash) : IMessage
{
    public static UnpackMessage Default(IFileInfo archive, IFileSystem fileSystem) => new UnpackMessage(archive, "*", [], fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(archive.Directory!.FullName, Constants.DefaultDir)), false, false, SupportedFormats.CanWriteSomehow, [ImageFormat.png], true, true, false);
}