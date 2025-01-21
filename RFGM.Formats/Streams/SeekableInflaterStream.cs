using System.Buffers;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace RFGM.Formats.Streams;

/// <summary>
/// Hack to enable seek on top of InflaterInputStream. Use under StreamView to maintain valid position. Does not hold resources and does not own underlying stream
/// </summary>
public class SeekableInflaterStream(Stream underlyingStream, long inflatedLength, string name) : Stream
{
    public string Name => name;

    public Stream UnderlyingStream { get; } = underlyingStream;

    private InflaterInputStream Inflater { get; set; } = new(underlyingStream){IsStreamOwner = false};

    private long ViewStart { get; } = underlyingStream.Position;

    public bool IsStreamOwner { get; set; } = false;

    private bool isClosed;

    public override void Flush()
    {
        Inflater.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Inflater.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        // NOTE: Position is taken from inflater and can't be trusted as it reads by blocks, so only Seek from Begin is possible
        switch (origin)
        {
            case SeekOrigin.Begin:
                if (offset < 0 || offset > Length)
                {
                    throw new InvalidOperationException($"Out of bounds: offset is {offset}, origin is {origin}, max length is {Length}");
                }
                return Rewind(offset);
            default:
                throw new NotSupportedException("Don't use this class directly, wrap it in StreamView instead");
        }
    }

    private long Rewind(long absolutePosition)
    {
        /*if (absolutePosition >= Position)
        {
            // inflater position is unreliable, fast-forward without reset to 0 results in garbage
            return FastForward(absolutePosition - Position);
        }*/

        // rebuild and fast-forward from start
        UnderlyingStream.Position = ViewStart;
        Inflater.Dispose();
        Inflater = new InflaterInputStream(UnderlyingStream){IsStreamOwner = false};
        return FastForward(absolutePosition);
    }

    public long FastForward(long length)
    {
        if (length == 0)
        {
            return 0;
        }


        var pool = ArrayPool<byte>.Shared;
        var buf = pool.Rent(chunkSize);
        var intLength = (int) length;
        var chunks = intLength / chunkSize;
        var remainder = intLength % chunkSize;
        for (int i = 0; i < chunks; i++)
        {
            var read = Inflater.Read(buf, 0, chunkSize);
            if (read != chunkSize)
            {
                throw new InvalidOperationException($"Inflater fast-forward returned wrong result [{read}], expected [{chunkSize}]. Streams: {this}");
            }
        }

        if (remainder != 0)
        {
            var read = Inflater.Read(buf, 0, remainder);
            if (read != remainder)
            {
                throw new InvalidOperationException($"Inflater fast-forward returned wrong result [{read}], expected [{remainder}]. Streams: {this}");
            }
        }

        pool.Return(buf);
        return length;
    }

    private readonly int chunkSize = 1 * 1024 * 1024;

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Can't change inflated size");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Can't write to inflater");
    }

    public override bool CanRead => UnderlyingStream.CanRead;
    public override bool CanSeek => UnderlyingStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => inflatedLength;

    public override long Position { get => Inflater.Position; set => throw new NotSupportedException("Can't directly set position, use Seek()"); }

    public override string ToString() => $"{nameof(SeekableInflaterStream)}({Name}, {UnderlyingStream})";
    //public override string ToString() => $"{nameof(SeekableInflaterStream),23} viewStart={ViewStart,10}, len={Length,10}, pos={Position,10}, owner={IsStreamOwner}, inflaterPos={Inflater.Position,10}\n\tunderlying={Utils.StreamToString(UnderlyingStream)}";

    protected override void Dispose(bool disposing)
    {
        if (isClosed)
        {
            return;
        }
        isClosed = true;
        if (IsStreamOwner)
        {
            UnderlyingStream.Dispose();
        }
    }
}
