using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Parsing;

public static class Helpers
{
    public static byte[] ReadBytesWithOffsets(Stream stream)
    {
        var start = stream.ReadUInt32();
        var end = stream.ReadUInt32();
        return stream.ReadBytes((int)(end - start) - 8);
    }

    public static void WriteBytesWithOffsets(Stream stream, byte[] data) => WriteWithOffsets(stream, _ => stream.WriteBytes(data));

    public static void WriteWithOffsets(Stream stream, Action<Stream> writeAction)
    {
        var beginningPos = stream.Position;
        stream.WriteUInt32(0);
        stream.WriteUInt32(0);
        writeAction(stream);
        
        var endPos = stream.Position;
        stream.Seek(beginningPos, SeekOrigin.Begin);
        stream.WriteUInt32((uint)beginningPos);
        stream.WriteUInt32((uint)endPos);
        stream.Seek(endPos, SeekOrigin.Begin);
    }
}