using System.Collections.Immutable;
using System.IO.Abstractions;

namespace RFGM.Formats.Streams;

/// <summary>
/// Pair of CPU+GPU files: cpeg_pc+gpeg_pc or other c*+g*
/// </summary>
/// <param name="Cpu"></param>
/// <param name="Gpu"></param>
public record PairedFiles(IFileInfo Cpu, IFileInfo Gpu, string Name)
{
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
    public static string? GetCpuFileName(string fileName) => RebuildExtension(fileName, "c");

    /// <summary>
    /// Works for full and relative filenames
    /// </summary>
    public static string? GetGpuFileName(string fileName) => RebuildExtension(fileName, "g");

    private static string? RebuildExtension(string fileName, string prefix)
    {
        var extWithDot = FormatUtils.GetLastExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extWithDot))
        {
            return null;
        }
        var ext = extWithDot[2..];
        return KnownPairExt.Contains(ext)
            ? Path.ChangeExtension(fileName, $"{prefix}{ext}")
            : null;
    }

    /// <summary>
    /// Pair extensions without c/g prefix
    /// </summary>
    private static readonly ImmutableHashSet<string> KnownPairExt = new HashSet<string>
    {
        "efct_pc",
        "chk_pc",
        "stch_pc",
        "smesh_pc",
        "vbm_pc",
        "peg_pc",
        "cmesh_pc",
        "car_pc",
        "terrain_pc",
        "tmesh_pc",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
}
