using System.Text;

namespace RFGM.Formats.Streams;

public class TracingStream(Stream x, string name):Stream
{
    private string State => $"> {name} len={Length} pos={Position}";

    public override void Flush()
    {
        Console.WriteLine($"{State} {nameof(Flush)}");
        x.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var sb = new StringBuilder($"{State} {nameof(Read)} buf[{buffer.Length}] offset={offset} count={count}");
        var result = x.Read(buffer, offset, count);
        Console.WriteLine(sb.Append($" newPos={Position} return={result} buf={Convert.ToHexString(buffer)}"));
        return result;
    }

    public override int ReadByte()
    {
        var oneByteArray = new byte[1];
        var r = Read(oneByteArray, 0, 1);
        return r == 0 ? -1 : oneByteArray[0];
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Console.WriteLine($"{State} {nameof(Seek)} offset={offset} origin={origin}");
        return x.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        Console.WriteLine($"{State} {nameof(SetLength)} val={value}");
        x.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Console.WriteLine($"{State} {nameof(Write)} buf[{buffer.Length}] offset={offset} count={count}");
        x.Write(buffer, offset, count);
    }

    public override bool CanRead => x.CanRead;

    public override bool CanSeek => x.CanSeek;

    public override bool CanWrite => x.CanWrite;

    public override long Length => x.Length;

    public override long Position
    {
        get => x.Position;
        set
        {
            Console.WriteLine($"{State} SetPosition val={value}");
            x.Position = value;
        }
    }
}