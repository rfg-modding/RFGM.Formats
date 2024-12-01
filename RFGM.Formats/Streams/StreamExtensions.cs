using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RFGM.Formats.Streams;

public static class StreamExtensions
{
    public static long AlignRead(this Stream stream, long alignment)
    {
        long paddingSize = stream.CalcAlignment(alignment);
        stream.Seek(paddingSize, SeekOrigin.Current);
        return paddingSize;
    }

    public static long CalcAlignment(this Stream stream, long alignment)
    {
        return stream.CalcAlignment(stream.Position, alignment);
    }
    
    public static long CalcAlignment(this Stream stream, long position, long alignment)
    {
        long remainder = position % alignment;
        long paddingSize = remainder > 0 ? alignment - remainder : 0;
        return paddingSize;
    }

    //TODO: Need to make sure C# doesn't do anything weird with struct data in memory like reordering variables. If so this approach will need to change
    public static unsafe T Read<T>(this Stream stream) where T : unmanaged
    {
        byte[] buffer = new byte[Unsafe.SizeOf<T>()];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        if (bytesRead != buffer.Length)
            throw new EndOfStreamException();

        fixed (byte* bufferStart = buffer)
        {
            T* ptr = (T*)bufferStart;
            T value = *ptr;
            return value;   
        }
    }

    public static void Skip(this Stream stream, long bytesToSkip)
    {
        stream.Seek(bytesToSkip, SeekOrigin.Current);
    }

    //TODO: Requires testing
    public static string ReadNullTerminatedString(this Stream stream)
    {
        string result = string.Empty;
        int c = stream.ReadByte();
        while (c != '\0')
        {
            if (c < 0)
                throw new EndOfStreamException();
            
            result += (char)c;
            c = stream.ReadByte();
        }

        return result;
    }
}