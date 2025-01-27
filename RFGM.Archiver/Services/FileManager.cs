using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Formats;

namespace RFGM.Archiver.Services;

public class FileManager(IFileSystem fileSystem, ILogger<FileManager> log)
{
    public IFileInfo CreateFileRecursive(IDirectoryInfo parent, string fileName, bool force) => CreateFileRecursive(fileSystem.Path.Combine(parent.FullName, fileName), force);

    public IFileInfo CreateFileRecursive(string path, bool force)
    {
        var dstFile = fileSystem.FileInfo.New(path);
        if (dstFile.Exists)
        {
            if (force)
            {
                log.LogTrace($"Delete file [{dstFile}]");
                dstFile.Delete();
                dstFile.Refresh();
            }
            else
            {
                throw new InvalidOperationException($"Destination file [{dstFile.FullName}] already exists! Use --force flag to overwrite");
            }
        }

        CreateDirectoryRecursive(dstFile.Directory!);
        log.LogTrace($"Create file {dstFile}");
        dstFile.Create().Close();
        dstFile.Refresh();

        return dstFile;
    }

    public IDirectoryInfo CreateDirectoryRecursive(IDirectoryInfo directoryInfo)
    {
        var stack = new Stack<IDirectoryInfo>();
        var current = directoryInfo;
        while (current != null)
        {
            stack.Push(current);
            current = current.Parent;
        }

        foreach (var x in stack.Where(x => !x.Exists))
        {
            log.LogTrace($"Create dir {directoryInfo}");
            x.Create();
        }

        return directoryInfo;
    }

    /// <summary>
    /// If file/folder should not be packed
    /// </summary>
    public bool IsIgnored(IFileSystemInfo item)
    {
        return item.Name == Constants.MetadataFile;
    }
}
