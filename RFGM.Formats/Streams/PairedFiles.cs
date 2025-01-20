using System.IO.Abstractions;

namespace RFGM.Formats.Streams;

/// <summary>
/// Pair of CPU+GPU files: cpeg_pc+gpeg_pc or other c*+g*
/// </summary>
/// <param name="Cpu"></param>
/// <param name="Gpu"></param>
public record PairedFiles(IFileInfo Cpu, IFileInfo Gpu, string Name)
{
    /// <summary>
    /// Opens a pair of CPU+GPU files from any one of them. Accounts for unpacked archives where order is prepended to file names
    /// </summary>
    public static PairedFiles? FromExistingFile(IFileInfo input)
    {
        if (!input.Exists)
        {
            return null;
        }

        var cpu = GetCpuFileName(input.FullName);
        var gpu = GetGpuFileName(input.FullName);
        if (cpu is null || gpu is null)
        {
            return null;
        }


        var cpuFile = LocateFile(input.FileSystem, cpu);
        var gpuFile = LocateFile(input.FileSystem, gpu);
        if (cpuFile is null || gpuFile is null)
        {
            return null;
        }

        var nameWithoutNumber = Utils.GetNameWithoutNumber(cpuFile.Name);

        return new(cpuFile, gpuFile, nameWithoutNumber);
    }

    private static IFileInfo? LocateFile(IFileSystem fileSystem, string fullPath)
    {
        var f = fileSystem.FileInfo.New(fullPath);
        if (f.Exists)
        {
            return f;
        }

        var nameWithoutNumber = Utils.GetNameWithoutNumber(f.Name);
        var fileWithOtherNumber = f.Directory!.EnumerateFiles().FirstOrDefault(x => x.Name.EndsWith(nameWithoutNumber));
        return fileWithOtherNumber;
    }

    public PairedStreams OpenRead()
    {
        var c = Cpu.OpenRead();
        var g = Gpu.OpenRead();
        return new PairedStreams(c, g);
    }

    public PairedStreams OpenWrite()
    {
        var c = Cpu.OpenWrite();
        var g = Gpu.OpenWrite();
        return new PairedStreams(c, g);
    }

    public string FullName => Cpu.FullName;

    /// <summary>
    /// Works for full and relative filenames
    /// </summary>
    public static string? GetCpuFileName(string fileName)
    {
        var extWithDot = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extWithDot))
        {
            return null;
        }
        var ext = extWithDot[1..];
        var cpuExt = ext switch
        {
            "cpeg_pc" => "cpeg_pc",
            "gpeg_pc" => "cpeg_pc",
            "cvbm_pc" => "cvbm_pc",
            "gvbm_pc" => "cvbm_pc",
            _ => null
        };
        return cpuExt is null
            ? null
            : Path.ChangeExtension(fileName, cpuExt);
    }

    /// <summary>
    /// Works for full and relative filenames
    /// </summary>
    public static string? GetGpuFileName(string fileName)
    {
        var extWithDot = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extWithDot))
        {
            return null;
        }
        var ext = extWithDot[1..];
        var gpuExt = ext switch
        {
            "cpeg_pc" => "gpeg_pc",
            "gpeg_pc" => "gpeg_pc",
            "cvbm_pc" => "gvbm_pc",
            "gvbm_pc" => "gvbm_pc",
            _ => null
        };
        return gpuExt is null
            ? null
            : Path.ChangeExtension(fileName, gpuExt);
    }
}
