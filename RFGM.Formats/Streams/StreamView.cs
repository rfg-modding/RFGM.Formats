using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace RFGM.Formats.Streams;

/// <summary>
/// Limited view of an underlying stream, maintaining its own position. Does not hold resources and does not own underlying stream
/// </summary>
public sealed class StreamView(Stream underlyingStream, long viewStart, long viewLength, string name)
    : Stream
{
    public bool IsStreamOwner { get; set; } = false;

    private bool isClosed;

    public string Name => name;

    public override bool CanRead => UnderlyingStream.CanRead;

    public override bool CanSeek => UnderlyingStream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; } = viewLength;

    public override long Position { get; set; } = 0;

    public Stream UnderlyingStream { get; } = underlyingStream;

    public long ViewStart { get; } = viewStart;

    public override void Flush() => UnderlyingStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        //Console.WriteLine($">>> READ ARGS {offset} {count} (view {name})");
        if (Position >= Length)
        {
            return 0;
        }

        // subtracting extra bytes to avoid overflow
        var extraBytes = Position + count >= Length
            ? (int) (Position + count - Length)
            : 0;

        if (UnderlyingStream.Position != ViewStart + Position)
        {
            //Console.WriteLine($">>> READ SEEK {ViewStart}+{Position} (view {name})");
            UnderlyingStream.Seek(ViewStart + Position, SeekOrigin.Begin);
        }

        //Console.WriteLine($">>> READ UNDR {offset} {count-extraBytes} (view {name})");
        var result = UnderlyingStream.Read(buffer, offset, count - extraBytes);
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

        if (UnderlyingStream.Position != ViewStart + Position)
        {
            UnderlyingStream.Seek(ViewStart + Position, SeekOrigin.Begin);
        }

        var result = UnderlyingStream.ReadByte();
        if (result > 0)
        {
            Position += result;
        }

        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        //Console.WriteLine($">>> SEEK VIEW {offset} {origin} (view {name})");
        switch (origin)
        {
            case SeekOrigin.Begin:
                if (offset < 0 || offset > Length)
                {
                    throw new InvalidOperationException($"Out of bounds: offset is {offset}, origin is {origin}, max length is {Length}");
                }

                Position = offset;
                return UnderlyingStream.Seek(ViewStart + offset, SeekOrigin.Begin);
            case SeekOrigin.Current:
                if (Position + offset < 0 || Position + offset > Length)
                {
                    throw new InvalidOperationException($"Out of bounds: offset is {offset}, position is {Position}, origin is {origin}, max length is {Length}");
                }

                Position += offset;
                return UnderlyingStream.Seek(Position, SeekOrigin.Begin);
            case SeekOrigin.End:
                if (offset > 0 || offset < -Length)
                {
                    throw new InvalidOperationException($"Out of bounds: offset is {offset}, origin is {origin}, max length is {Length}");
                }

                Position = Length + offset;
                return UnderlyingStream.Seek(Position, SeekOrigin.Begin);
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }
    }

    public override void SetLength(long value) => throw new InvalidOperationException($"{nameof(StreamView)} is read-only");

    public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException($"{nameof(StreamView)} is read-only");

    public override string ToString() => $"{nameof(StreamView),23} viewStart={ViewStart,10}, len={Length,10}, pos={Position,10}, owner={IsStreamOwner}, {name,20}\n\tunderlying={UnderlyingStream}";

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
