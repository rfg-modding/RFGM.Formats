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
    public bool IsStreamOwner { get; init; } = false;

    private bool isClosed;
    private long position = 0;

    public string Name => name;

    public override bool CanRead => UnderlyingStream.CanRead;

    public override bool CanSeek => UnderlyingStream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; } = viewLength;

    public override long Position
    {
        get => position;
        set
        {
            lock (UnderlyingStream)
            {
                position = value;
            }
        }
    }

    public Stream UnderlyingStream { get; } = underlyingStream;

    public long ViewStart { get; } = viewStart;

    public override void Flush() => UnderlyingStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        lock (UnderlyingStream)
        {
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
                if (UnderlyingStream is not InflaterInputStream)
                {
                    UnderlyingStream.Seek(ViewStart + Position, SeekOrigin.Begin);
                }
            }

            var result = UnderlyingStream.Read(buffer, offset, count - extraBytes);
            if (result > 0)
            {
                Position += result;
            }

            return result;
        }
    }

    public override int ReadByte()
    {
        var oneByteArray = new byte[1];
        int r = Read(oneByteArray, 0, 1);
        return r == 0 ? -1 : oneByteArray[0];
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        lock (UnderlyingStream)
        {
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
    }


    public override void SetLength(long value) => throw new InvalidOperationException($"{nameof(StreamView)} is read-only");

    public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException($"{nameof(StreamView)} is read-only");

    public override string ToString() => $"{nameof(StreamView)}({Name}, {UnderlyingStream})";
    //public override string ToString() => $"{nameof(StreamView),23} viewStart={ViewStart,10}, len={Length,10}, pos={Position,10}, owner={IsStreamOwner}, {name,20}\n\tunderlying={Utils.StreamToString(UnderlyingStream)}";

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

    private string State => $"> {name} len={Length} pos={Position}";

}
