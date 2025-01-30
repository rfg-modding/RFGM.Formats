using System.Buffers;
using System.Globalization;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace RFGM.Formats.Streams;

/// <summary>
/// A version of StreamView that owns the underlying and stream and disposes of it.
/// </summary>
public sealed class OwningStreamView(Stream stream, long viewStart, long viewLength)
    : Stream
{
    public override bool CanRead => Stream.CanRead;

    public override bool CanSeek => Stream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; } = viewLength;

    public override long Position { get; set; }

    private Stream Stream { get; } = stream;

    private long ViewStart { get; } = viewStart;
    
    public OwningStreamView ThreadSafeCopy()
    {
        switch (Stream)
        {
            case FileStream fs:
                var newFileStream = File.OpenRead(fs.Name);
                newFileStream.Position = fs.Position;
                var result1 = new OwningStreamView(newFileStream, ViewStart, Length);
                result1.Position = Position;
                return result1;
            case MemoryStream ms:
                var newMemoryStream = new MemoryStream(ms.ToArray());
                newMemoryStream.Position = ms.Position;
                var result2 = new OwningStreamView(newMemoryStream, ViewStart, Length);
                result2.Position = Position;
                return result2;
            case InflaterInputStream iis:
                var inflated = new MemoryStream();
                iis.CopyTo(inflated);
                var result3 = new OwningStreamView(inflated, ViewStart, Length);
                result3.Position = Position;
                return result3;
            case OwningStreamView sv:
                var innerCopy = sv.ThreadSafeCopy().Stream;
                var result4 = new OwningStreamView(innerCopy, ViewStart, Length);
                result4.Position = Position;
                return result4;
            default:
                throw new InvalidOperationException();
        }
    }

    public override void Flush() => Stream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Position >= Length)
        {
            return 0;
        }

        // subtracting extra bytes to avoid overflow
        var extraBytes = Position + count >= Length
            ? (int) (Position + count - Length)
            : 0;

        if (Stream.Position != ViewStart + Position)
        {
            if (Stream is not InflaterInputStream)
            {
                Stream.Seek(ViewStart + Position, SeekOrigin.Begin);
            }
        }

        var result = Stream.Read(buffer, offset, count - extraBytes);
        if (result > 0)
        {
            Position += result;
        }

        return result;
    }

    public override int ReadByte()
    {
        if (Position + 1 >= ViewStart + Length)
        {
            return -1;
        }

        if (Stream.Position != ViewStart + Position)
        {
            Stream.Seek(ViewStart + Position, SeekOrigin.Begin);
        }

        var result = Stream.ReadByte();
        if (result > 0)
        {
            Position += result;
        }

        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                if (offset < 0 || offset > Length)
                {
                    throw new InvalidOperationException($"Out of bounds: offset is {offset}, origin is {origin}, max length is {Length}");
                }

                if (Stream is InflaterInputStream)
                {
                    // hack to avoid seeking but still allow fast-forwarding
                    var delta = (int) (offset - Position);
                    if (delta < 0)
                    {
                        throw new InvalidOperationException("Can't seek back to rewind InflaterInputStream");
                    }

                    var pool = ArrayPool<byte>.Shared;
                    var buf = pool.Rent(delta);
                    Position = offset;
                    var read = Stream.Read(buf, 0, delta);
                    pool.Return(buf);
                    return read;
                }

                Position = offset;
                return Stream.Seek(ViewStart + offset, SeekOrigin.Begin);
            case SeekOrigin.Current:
                if (Position + offset < 0 || Position + offset > Length)
                {
                    throw new InvalidOperationException($"Out of bounds: offset is {offset}, position is {Position}, origin is {origin}, max length is {Length}");
                }

                Position += offset;
                return Stream.Seek(offset, SeekOrigin.Current);
            case SeekOrigin.End:
                if (offset < 0 || offset > Length)
                {
                    throw new InvalidOperationException($"Out of bounds: offset is {offset}, origin is {origin}, max length is {Length}");
                }

                Position = Length - offset;
                return Stream.Seek(ViewStart + Length - offset, SeekOrigin.Begin);
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }
    }

    public override void SetLength(long value) => throw new InvalidOperationException($"{nameof(OwningStreamView)} is read-only");

    public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException($"{nameof(OwningStreamView)} is read-only");

    public override string ToString()
    {
        var length = Stream is InflaterInputStream
            ? "unsupported (inflater stream)"
            : Stream.Length.ToString(CultureInfo.InvariantCulture);
        return $"OwningStreamView: start={ViewStart}, len={Length}, pos={Position}, stream len={length}, stream pos={Stream.Position}";
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Stream.Dispose();
    }
}
