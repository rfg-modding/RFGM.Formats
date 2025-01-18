using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct Link()
{
    public int YieldMax = 0;
    public float Area = 0;
    public short[] Obj = new short[2];
    public byte Flags = 0;

    private const long SizeInFile = 16;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        YieldMax = stream.ReadInt32();
        Area = stream.ReadFloat();
        Obj[0] = stream.ReadInt16();
        Obj[1] = stream.ReadInt16();
        Flags = stream.ReadUInt8();
        stream.Skip(3);
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Link. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}