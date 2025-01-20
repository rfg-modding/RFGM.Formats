using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Peg;

public class PegWriter(LogicalTextureArchive logicalTextureArchive)
    : IDisposable
{
    private readonly byte[] headerMagic =
    {
        71, 69, 75, 86
    };

    private readonly byte[] headerVersion =
    {
        0x0A, 0
    };

    private readonly List<LogicalTexture> logicalTextures = logicalTextureArchive.LogicalTextures.ToList();

    public async Task WriteAll(Stream destinationCpu, Stream destinationGpu, CancellationToken token)
    {
        BinUtils.CheckStream(destinationCpu);
        BinUtils.CheckStream(destinationGpu);
        CheckEntries(token);
        var entryNamesBlock = await GetEntryNamesUpdateOffsets(token);
        var headerBlockSize = 24;
        // occupy space for later overwriting
        await BinUtils.WriteZeroes(destinationCpu, headerBlockSize, token);
        foreach (var logicalTexture in logicalTextures)
        {
            await WriteEntry(logicalTexture, destinationCpu, destinationGpu, token);
        }
        await BinUtils.Write(destinationCpu, entryNamesBlock, token);

        var header = await GetHeader(destinationCpu.Position, destinationGpu.Position, token);
        destinationCpu.Position = 0;
        await BinUtils.Write(destinationCpu, header, token);
    }

    private async Task<byte[]> GetHeader(long cpuSize, long gpuSize, CancellationToken token)
    {
        await using var ms = new MemoryStream();
        await BinUtils.Write(ms, headerMagic, token);
        await BinUtils.Write(ms, headerVersion, token);
        await BinUtils.WriteUint2(ms, 0, token);
        await BinUtils.WriteUint4(ms, cpuSize, token);
        await BinUtils.WriteUint4(ms, gpuSize, token);
        await BinUtils.WriteUint2(ms, logicalTextures.Count, token);
        await BinUtils.WriteUint2(ms, 0, token);
        await BinUtils.WriteUint2(ms, logicalTextures.Count, token);
        await BinUtils.WriteUint2(ms, logicalTextureArchive.Align, token);
        return ms.ToArray();
    }

    private async Task WriteEntry(LogicalTexture logicalTexture, Stream destinationCpu, Stream destinationGpu, CancellationToken token)
    {
        await BinUtils.WriteUint4(destinationCpu, destinationGpu.Position, token);
        await BinUtils.WriteUint2(destinationCpu, logicalTexture.Size.Width, token);
        await BinUtils.WriteUint2(destinationCpu, logicalTexture.Size.Height, token);
        await BinUtils.WriteUint2(destinationCpu, (int)logicalTexture.Format, token);
        await BinUtils.WriteUint2(destinationCpu, logicalTexture.Source.Width, token);
        await BinUtils.WriteUint2(destinationCpu, logicalTexture.AnimTiles.Width, token);
        await BinUtils.WriteUint2(destinationCpu, logicalTexture.AnimTiles.Height, token);
        await BinUtils.WriteUint2(destinationCpu, 1, token);
        await BinUtils.WriteUint2(destinationCpu, (int)logicalTexture.Flags, token);
        await BinUtils.WriteUint4(destinationCpu, logicalTexture.NameOffset, token);
        await BinUtils.WriteUint2(destinationCpu, logicalTexture.Source.Height, token);
        await BinUtils.WriteUint1(destinationCpu, 1, token);
        await BinUtils.WriteUint1(destinationCpu, logicalTexture.MipLevels, token);
        await BinUtils.WriteUint4(destinationCpu, logicalTexture.Data.Length, token);
        await BinUtils.WriteUint4(destinationCpu, 0, token);
        await BinUtils.WriteUint4(destinationCpu, 0, token);
        await BinUtils.WriteUint4(destinationCpu, 0, token);
        await BinUtils.WriteUint4(destinationCpu, 0, token);

        // finally write data to gpu file
        await BinUtils.WriteStream(destinationGpu, logicalTexture.Data, token);
        await BinUtils.WriteZeroes(destinationGpu, logicalTexture.PadSize, token);
    }

    private async Task<byte[]> GetEntryNamesUpdateOffsets(CancellationToken token)
    {
        if (logicalTextures.Count == 0)
        {
            return Array.Empty<byte>();
        }

        await using var ms = new MemoryStream();
        foreach (var logicalTexture in logicalTextures)
        {
            token.ThrowIfCancellationRequested();
            logicalTexture.NameOffset = (int)ms.Position;
            await BinUtils.Write(ms, logicalTexture.GetNameCString(), token);
        }

        return ms.ToArray();
    }


    private void CheckEntries(CancellationToken token)
    {
        var i = 0;
        foreach (var logicalTexture in logicalTextures)
        {
            token.ThrowIfCancellationRequested();
            if (logicalTexture.Order != i)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalTexture), logicalTexture.Order, $"Invalid order, expected {i}");
            }

            if (string.IsNullOrWhiteSpace(logicalTexture.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(logicalTexture), logicalTexture.Name, "Invalid name, expected meaningful string");
            }

            if (logicalTexture.Align != logicalTextureArchive.Align)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalTexture), logicalTexture.Align, "Invalid align, expected same as in archive header");
            }

            i++;
        }
    }

    public void Dispose() =>
        // GC magic!
        logicalTextures.Clear();
}
