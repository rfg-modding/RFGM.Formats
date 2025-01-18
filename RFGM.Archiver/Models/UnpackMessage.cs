using System.Collections.Immutable;
using System.IO.Abstractions;

namespace RFGM.Archiver.Models;

public record UnpackMessage(IFileInfo Archive, string FileGlob, IReadOnlyList<string> RelativePath, IDirectoryInfo OutputPath, bool XmlFormat, bool Recursive, ImmutableHashSet<FileFormat> Formats, ImmutableHashSet<TextureFormat> Textures, bool Metadata, bool Force, bool Hash) : IMessage
{
    public static UnpackMessage Default(IFileInfo archive, IFileSystem fileSystem) => new UnpackMessage(archive, "*", [], fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(archive.Directory!.FullName, Constatns.DefaultDir)), false, false, SupportedFormats.CanWriteSomehow, [TextureFormat.PNG], true, true, false);
}

public interface IMetadata;

public record VppArchive(string Name, string RelativePath, string Mode, long Size, string Hash, int EntryCount) : IMetadata;
public record VppEntry(string Name, string RelativePath, int Order, uint Offset, long Size, uint CompressedSize, string Hash) : IMetadata;
