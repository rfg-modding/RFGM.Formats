using System.IO.Abstractions;
using RFGM.Archiver.Models.Metadata;
using RFGM.Formats;

namespace RFGM.Archiver.Models.Messages;

public record PackMessage(IDirectoryInfo Source, Breadcrumbs Breadcrumbs, IDirectoryInfo OutputPath, bool Recursive, bool Metadata, bool Hash, bool Force) : IMessage
{
    public static PackMessage Default(IDirectoryInfo path, IFileSystem fileSystem) => new (path, Breadcrumbs.Init(), fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(path.Parent!.FullName, Constants.DefaultOutputDir)), false, true, false, true);
}
