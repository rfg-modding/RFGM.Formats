using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Shaders;

public struct ConstFixup
{
    public uint CrcValue; //TODO: Should this be an int? Is according to the game. But all the other CRCs are uints
    public byte ConstLoc;
    public byte Flags;
    public byte NumRows;
    public byte ArraySize;

    public string Name => HashDictionary.FindOriginString(CrcValue) ?? "Unknown";
    
    private const long SizeInFile = 8;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        CrcValue = stream.ReadUInt32();
        ConstLoc = stream.ReadUInt8();
        Flags = stream.ReadUInt8();
        NumRows = stream.ReadUInt8();
        ArraySize = stream.ReadUInt8();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ConstFixup. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}