using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RFGM.Formats;

namespace RFGM.Archiver.Services;

public class FileManager(IFileSystem fileSystem, ILogger<FileManager> log)
{
    public IFileInfo CreateFile(IDirectoryInfo parent, string fileName, bool force)
    {
        var dstFilePath = fileSystem.Path.Combine(parent.FullName, fileName);
        var dstFile = fileSystem.FileInfo.New(dstFilePath);
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

        return dstFile;
    }

    public IDirectoryInfo CreateSubDirectory(IDirectoryInfo parent, string dirName)
    {
        var path = fileSystem.Path.Combine(parent.FullName, dirName);
        var result = fileSystem.DirectoryInfo.New(path);
        if (!result.Exists)
        {
            log.LogTrace($"Create dir {result}");
            result.Create();
        }

        return result;
    }

    public IDirectoryInfo CreateDirectory(IDirectoryInfo directoryInfo)
    {
        if (!directoryInfo.Exists)
        {
            log.LogTrace($"Create dir {directoryInfo}");
            directoryInfo.Create();
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
