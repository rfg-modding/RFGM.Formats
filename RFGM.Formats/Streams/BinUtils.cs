using System.Text;

namespace RFGM.Formats.Streams;

/// <summary>
/// Methods to work with precise binary representation of different values in a stream
/// </summary>
public static class BinUtils
{
    /// <summary>
    /// Represent bytes as DEADBEEF-like string, optionally with separator between bytes
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string ToHexString(byte[] bytes, string separator = "") => BitConverter.ToString(bytes).Replace("-", separator);

    /// <summary>
    /// Ensure stream is in start position, has nonzero length, is writable and seekable. Throw ArgumentException otherwise
    /// </summary>
    public static void CheckStream(Stream s)
    {
        if (!s.CanSeek)
        {
            throw new ArgumentException($"Need seekable stream, got {s}", nameof(s));
        }

        if (!s.CanWrite)
        {
            throw new ArgumentException($"Need writable stream, got {s}", nameof(s));
        }

        if (s.Position != 0)
        {
            throw new ArgumentException($"Expected start of stream, got position = {s.Position}", nameof(s));
        }

        if (s.Length != 0)
        {
            throw new ArgumentException($"Expected empty stream, got length = {s.Length}", nameof(s));
        }
    }

    /// <summary>
    /// Write data from stream
    /// </summary>
    public static async Task WriteStream(Stream stream, Stream src, CancellationToken token) => await src.CopyToAsync(stream, token);

    /// <summary>
    /// Write data from byte array
    /// </summary>
    public static async Task Write(Stream stream, byte[] value, CancellationToken token) => await stream.WriteAsync(value, token);

    /// <summary>
    /// Write required amount of zeroes
    /// </summary>
    public static async Task WriteZeroes(Stream stream, int count, CancellationToken token)
    {
        if (count == 0)
        {
            return;
        }

        var value = new byte[count];
        await stream.WriteAsync(value, token);
    }

    /// <summary>
    /// Write value as unsigned int, little endian
    /// </summary>
    public static async Task WriteUint4(Stream stream, long value, CancellationToken token) => await Write(stream, BitConverter.GetBytes((uint) value), token);

    /// <summary>
    /// Write value as unsigned int, little endian
    /// </summary>
    public static async Task WriteUint4(Stream stream, int value, CancellationToken token) => await Write(stream, BitConverter.GetBytes((uint) value), token);

    /// <summary>
    /// Write value as unsigned short, little endian
    /// </summary>
    public static async Task WriteUint2(Stream stream, int value, CancellationToken token) => await Write(stream, BitConverter.GetBytes((ushort) value), token);

    /// <summary>
    /// Write value as single byte
    /// </summary>
    public static Task WriteUint1(Stream stream, int value, CancellationToken _)
    {
        stream.WriteByte((byte) value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Write value as C-style string. String is trimmed or expanded to targetSize. Last char is always seto to zero terminator
    /// </summary>
    public static async Task WriteString(Stream stream, string value, int targetSize, CancellationToken token)
    {
        var chars = Encoding.ASCII.GetBytes(value + "\0");
        var fixedSizeChars = GetArrayOfFixedSize(chars, targetSize);
        // string must always end with \0 character!
        fixedSizeChars[targetSize - 1] = 0;
        await stream.WriteAsync(fixedSizeChars, token);
    }

    /// <summary>
    /// Trim or expand byte array to desired size
    /// </summary>
    public static byte[] GetArrayOfFixedSize(byte[] source, int targetSize)
    {
        var destination = new byte[targetSize];
        Array.Copy(source, destination, System.Math.Min(source.Length, destination.Length));
        return destination;
    }
}
