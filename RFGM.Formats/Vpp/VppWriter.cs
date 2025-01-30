using System.Globalization;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using RFGM.Formats.Streams;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Vpp;

public sealed class VppWriter(LogicalArchive logicalArchive)
    : IDisposable
{
    private readonly IList<LogicalFile> logicalFiles = logicalArchive.LogicalFiles.ToList(); // TODO optimize for streaming?
    private readonly RfgVpp.HeaderBlock.Mode mode = logicalArchive.Mode;

    private readonly byte[] headerMagic =
    {
        206,
        10,
        137,
        81
    };

    private readonly byte[] headerVersion =
    {
        3,
        0,
        0,
        0
    };

    public void Dispose() =>
        // GC magic!
        logicalFiles.Clear();

    public async Task WriteAll(Stream s, CancellationToken token)
    {
        BinUtils.CheckStream(s);
        CheckEntries(token);

        // this is only to get entries block size. offsets and sizes are not computed yet
        var fakeEntriesBlock = await GetEntries(token);
        var entriesPad = RfgVpp.GetPadSize(fakeEntriesBlock.LongLength, 2048, false);

        var entryNamesBlock = await GetEntryNames(token);
        var entryNamesPad = RfgVpp.GetPadSize(entryNamesBlock.LongLength, 2048, false);

        var entriesBlockSize = fakeEntriesBlock.Length + entriesPad;
        var entryNamesBlockSize = entryNamesBlock.Length + entryNamesPad;

        var headerBlockSize = 2048;

        // occupy space for later overwriting
        await BinUtils.WriteZeroes(s, headerBlockSize + entriesBlockSize + entryNamesBlockSize, token);

        var (dataSize, dataCompressedSize) = await WriteDataDetectProfile(s, token);

        var headerBlock = await GetHeader(s.Length, fakeEntriesBlock.Length, entryNamesBlock.Length, dataSize, dataCompressedSize, token);
        var realEntriesBlock = await GetEntries(token);
        s.Position = 0;
        await BinUtils.Write(s, headerBlock, token);
        await BinUtils.Write(s, realEntriesBlock, token);
        await BinUtils.WriteZeroes(s, entriesPad, token);
        await BinUtils.Write(s, entryNamesBlock, token);
        await BinUtils.WriteZeroes(s, entryNamesPad, token);
    }

    public void CheckEntries(CancellationToken token)
    {
        var i = 0;
        foreach (var logicalFile in logicalFiles)
        {
            token.ThrowIfCancellationRequested();
            if (logicalFile.Order != i)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalFile), logicalFile.Order, $"Invalid order, expected {i}");
            }

            if (string.IsNullOrWhiteSpace(logicalFile.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(logicalFile), logicalFile.Name, "Invalid name, expected meaningful string");
            }

            if (string.IsNullOrWhiteSpace(logicalArchive.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(logicalFile), logicalArchive.Name, "Invalid container name, expected meaningful string");
            }

            i++;
        }
    }

    public async Task<byte[]> GetEntries(CancellationToken token)
    {
        if (logicalFiles.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var currentNameOffset = 0;
        await using var ms = new MemoryStream();
        foreach (var logicalFile in logicalFiles)
        {
            token.ThrowIfCancellationRequested();
            var nameOffset = currentNameOffset;
            var dataOffset = logicalFile.Offset;
            // NOTE: no idea how to compute compressed size when compacted
            var compressedDataSize = logicalFile.CompressedSize;
            var hash = CircularHash(logicalFile.Name);

            await BinUtils.WriteUint4(ms, nameOffset, token);
            await BinUtils.WriteZeroes(ms, 4, token);
            await BinUtils.WriteUint4(ms, dataOffset, token);
            await BinUtils.Write(ms, hash, token);
            await BinUtils.WriteUint4(ms, logicalFile.Content.Length, token);
            await BinUtils.WriteUint4(ms, compressedDataSize, token);
            await BinUtils.WriteZeroes(ms, 4, token);
            currentNameOffset += logicalFile.NameCString.Value.Length; // names just go one after another
        }

        return ms.ToArray();
    }

    public async Task<byte[]> GetEntryNames(CancellationToken token)
    {
        if (logicalFiles.Count == 0)
        {
            return Array.Empty<byte>();
        }

        await using var ms = new MemoryStream();
        foreach (var logicalFile in logicalFiles)
        {
            token.ThrowIfCancellationRequested();
            await BinUtils.Write(ms, logicalFile.NameCString.Value, token);
        }

        return ms.ToArray();
    }

    public async Task<byte[]> GetHeader(long totalSize, int entryBlockLength, int nameBlockLength, uint dataBlockLength, uint compDataBlockLength, CancellationToken token)
    {
        /*
            NOTE: file length is set to 0xFFFFFF for very large archives
        */
        var buffer = new byte[2048];
        await using var ms = new MemoryStream(buffer);
        await BinUtils.Write(ms, headerMagic, token);
        await BinUtils.Write(ms, headerVersion, token);
        await BinUtils.WriteString(ms, "            Packed           with         SyncFaction            ", 65, token);
        await BinUtils.WriteString(ms, "           code by           rast         specs  by         moneyl       parsed with      kaitai.io     special thanks       Camo                                          Read The         Martian     Chronicles from  Ray Bradbury.   I liked them.         ", 256, token);
        await BinUtils.WriteZeroes(ms, 3, token);
        await BinUtils.Write(ms,
            new byte[]
            {
                (byte) mode,
                0,
                0,
                0
            },
            token);
        await BinUtils.WriteZeroes(ms, 4, token);
        await BinUtils.WriteUint4(ms, logicalFiles.Count, token);
        await BinUtils.WriteUint4(ms, totalSize, token);
        await BinUtils.WriteUint4(ms, entryBlockLength, token);
        await BinUtils.WriteUint4(ms, nameBlockLength, token);
        await BinUtils.WriteUint4(ms, dataBlockLength, token);
        await BinUtils.WriteUint4(ms, compDataBlockLength, token);
        return buffer;
    }

    private async Task<(uint size, uint compressedSize)> WriteDataDetectProfile(Stream s, CancellationToken token)
    {
        /*
        profiles:
            * str2 (both flags, 0)
            * normal vpp (no flags, 2048)
            * compressed vpp (compressed, 2048)
            * compacted vpp (both flags, 16)
        NOTE: how to get compDataSize for compressed-only mode?
        */

        var ext = FormatUtils.GetLastExtension(logicalArchive.Name).ToLower(CultureInfo.InvariantCulture);
        // all str2 are the same
        if (ext == ".str2_pc")
        {
            // TODO sometimes repacking is not enough and crashes the game. probably need to alter asm_pc file or do magic with offsets inside zlib stream
            return await WriteDataInternal(s, false, true, 9, 0, token);
        }

        // vpp can be different
        return mode switch
        {
            RfgVpp.HeaderBlock.Mode.Normal => await WriteDataInternal(s, false, false, 0, 2048, token),
            RfgVpp.HeaderBlock.Mode.Compressed => await WriteDataInternal(s, true, false, 9, 2048, token),
            RfgVpp.HeaderBlock.Mode.Compacted => await WriteDataInternal(s, false, true, 9, 16, token),
            RfgVpp.HeaderBlock.Mode.Condensed => throw new InvalidOperationException("Condensed-only mode is not present in vanilla files and is not supported"),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unexpected mode")
        };
    }

    private async Task<(uint size, uint compressedSize)> WriteDataInternal(Stream s, bool compressIndividual, bool compressOutput, int compressionLevel, int individualAlignment, CancellationToken token)
    {
        Func<Stream, Stream> wrapperFactory = compressOutput switch
        {
            true => x => new DeflaterOutputStream(x, new Deflater(compressionLevel)) { IsStreamOwner = false },
            false => static x => new DisposableStreamWrapper(x)
        };

        uint uncompressedSize = 0;
        uint offset = 0;
        var i = 0;
        var initialPosition = s.Position;
        await using (var output = wrapperFactory(s))
        {
            foreach (var logicalFile in logicalFiles)
            {
                token.ThrowIfCancellationRequested();
                logicalFile.Offset = offset;
                var posBefore = output.Position;
                if (compressIndividual)
                {
                    if (logicalFile.CompressedContent is null)
                    {
                        await CompressZlib(logicalFile.Content, compressionLevel, output, token);
                    }
                    else
                    {
                        await BinUtils.WriteStream(output, logicalFile.CompressedContent, token);
                    }

                    logicalFile.CompressedSize = (uint) (output.Position - posBefore);
                    offset += logicalFile.CompressedSize;
                }
                else
                {
                    await BinUtils.WriteStream(output, logicalFile.Content, token);
                    await output.FlushAsync(token);
                    offset += (uint) logicalFile.Content.Length;
                    if (compressOutput)
                    {
                        logicalFile.CompressedSize = (uint) (output.Position - posBefore);
                    }
                }

                uncompressedSize += (uint) logicalFile.Content.Length;
                if (i < logicalFiles.Count - 1)
                {
                    // align if not last entry
                    var padSize = RfgVpp.GetPadSize(offset, individualAlignment, false);
                    await BinUtils.WriteZeroes(output, padSize, token);
                    await output.FlushAsync(token);
                    offset += (uint) padSize;
                    uncompressedSize += (uint) padSize; // TODO is this legit?
                }

                i++;
            }
        }

        var delta = s.Position - initialPosition;
        var compressedSize = compressIndividual || compressOutput
            ? (uint) delta
            : 0xFFFFFFu;
        return (uncompressedSize, compressedSize);
    }

    public static byte[] CircularHash(string input)
    {
        input = input.ToLowerInvariant();

        uint hash = 0;
        for (var i = 0; i < input.Length; i++)
        {
            // rotate left by 6
            hash = (hash << 6) | (hash >> (32 - 6));
            hash = input[i] ^ hash;
        }

        var result = hash;
        return BitConverter.GetBytes(result);
    }

    private static async Task CompressZlib(Stream src, int compressionLevel, Stream destinationStream, CancellationToken token)
    {
        await using var deflater = new DeflaterOutputStream(destinationStream, new Deflater(compressionLevel)) { IsStreamOwner = false };
        await src.CopyToAsync(deflater, token);
    }
}
