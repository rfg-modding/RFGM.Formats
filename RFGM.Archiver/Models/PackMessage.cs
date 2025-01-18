using System.Collections.Immutable;
using System.IO.Abstractions;

namespace RFGM.Archiver.Models;

public record PackMessage(IDirectoryInfo Path, string FileGlob, IDirectoryInfo OutputPath, bool Recursive, ImmutableHashSet<FileFormat> Formats, bool Force) : IMessage
{
    public static PackMessage Default(IDirectoryInfo path, IFileSystem fileSystem) => new (path, "*", fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(path.Parent!.FullName, Constatns.DefaultOutputDir)), true, SupportedFormats.CanWriteSomehow, true);
}
