using System.Collections.Immutable;
using System.IO.Abstractions;
using RFGM.Formats;

namespace RFGM.Archiver.Models;

public record PackMessage(IDirectoryInfo Source, IReadOnlyList<string> RelativePath, IDirectoryInfo OutputPath, bool Recursive, bool Metadata, bool Hash, bool Force) : IMessage
{
    public static PackMessage Default(IDirectoryInfo path, IFileSystem fileSystem) => new (path, [], fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(path.Parent!.FullName, Constants.DefaultOutputDir)), false, true, false, true);
}
