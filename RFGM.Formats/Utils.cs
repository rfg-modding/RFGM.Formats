using System.IO.Abstractions;
using System.Security.Cryptography;
using RFGM.Formats.Streams;

namespace RFGM.Formats;

public static class Utils
{
    public static async Task<string> ComputeHash(IFileInfo file)
    {
        await using var s = file.OpenRead();
        return await ComputeHash(s);
    }

    public static async Task<string> ComputeHash(PairedFiles pairedFiles)
    {
        var cpuHash = await ComputeHash(pairedFiles.Cpu);
        var gpuHash = await ComputeHash(pairedFiles.Gpu);
        return $"{cpuHash}_{gpuHash}";
    }

    public static async Task<string> ComputeHash(PairedStreams pairedStreams)
    {
        var cpuHash = await ComputeHash(pairedStreams.Cpu);
        var gpuHash = await ComputeHash(pairedStreams.Gpu);
        return $"{cpuHash}_{gpuHash}";
    }

    /// <summary>
    /// Expects stream at position 0. Rewinds stream to 0. Does not close stream.
    /// </summary>
    public static async Task<string> ComputeHash(Stream s)
    {
        if (!s.CanSeek)
        {
            throw new ArgumentException($"Need seekable stream, got {s}", nameof(s));
        }

        if (!s.CanRead)
        {
            throw new ArgumentException($"Need readable stream, got {s}", nameof(s));
        }

        if (s.Position != 0)
        {
            throw new ArgumentException($"Expected start of stream, got position = {s.Position}", nameof(s));
        }

        using var sha = SHA256.Create();
        var hashValue = await sha.ComputeHashAsync(s);
        var hash = BitConverter.ToString(hashValue).Replace("-", "");
        s.Seek(0, SeekOrigin.Begin);
        return hash;
    }

    /// <summary>
    /// Get rid of order prefix, when filename is "00001 filename.ext"
    /// </summary>
    public static string GetNameWithoutNumber(string fileName)
    {
        var match = Constants.VppEntryNameFormat.Match(fileName);
        return match.Success
            ? match.Groups["name"].Value
            : fileName;
    }
}
