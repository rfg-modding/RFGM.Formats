using RFGM.Formats.Streams;

namespace RFGM.Formats.Asset;

public class AsmPrimitive
{
    public string Name = string.Empty;
    public PrimitiveType Type;
    public AllocatorType Allocator;
    public PrimitiveFlags Flags;
    public byte SplitExtIndex;
    public int HeaderSize;
    public int DataSize;

    public bool Read(Stream stream, out string error)
    {
        error = string.Empty;

        int nameLength = stream.ReadUInt16();
        Name = stream.ReadAsciiString(nameLength);
        Type = (PrimitiveType)stream.ReadUInt8();
        Allocator = (AllocatorType)stream.ReadUInt8();
        Flags = (PrimitiveFlags)stream.ReadUInt8();
        SplitExtIndex = stream.ReadUInt8();
        HeaderSize = stream.ReadInt32();
        DataSize = stream.ReadInt32();
        
        return true;
    }

    public bool Write(Stream stream, out string error)
    {
        Console.WriteLine($"Writing {Name}");
        error = string.Empty;
        if (Name.Length > ushort.MaxValue)
        {
            error = $"Exceeded max primitive name length of {ushort.MaxValue}.";
            return false;
        }
        
        stream.WriteUInt16((ushort)Name.Length);
        stream.WriteAsciiString(Name);
        stream.WriteUInt8((byte)Type);
        stream.WriteUInt8((byte)Allocator);
        stream.WriteUInt8((byte)Flags);
        stream.WriteUInt8(SplitExtIndex);
        stream.WriteInt32(HeaderSize);
        stream.WriteInt32(DataSize);

        return true;
    }
}