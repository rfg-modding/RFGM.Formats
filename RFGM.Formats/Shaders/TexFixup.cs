using System.Runtime.InteropServices;
using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Shaders;

public struct TexFixup
{
    //This is a union
    [StructLayout(LayoutKind.Explicit)]
    public struct ConstLocData
    {
        [FieldOffset(0)] public byte ConstLocDisk;
        [FieldOffset(0)] public byte Data; //Note: This is a bitfield where the first 5 bits are labelled "const_loc" and the last 3 are labelled "border_color" 
    }

    public uint CrcValue; //TODO: Should this be an int? Is according to the game. But all the other CRCs are uints
    public ConstLocData ConstLoc;
    public byte Flags;
    public byte AddressU;
    public byte AddressV;
    public byte AddressW;
    public byte MinFilter; //TODO: Could these and the address values be the same as the similarly named DirectX11 field options? Look at Texture_filter_mode_mapping_keen
    public byte MagFilter;
    public byte MipFilter;
    public byte ColorData; //The game has a union for this that is 4 bytes long. Only the first byte is used
    
    //TODO: Put other fields

    public string Name => HashDictionary.FindOriginString(CrcValue) ?? "Unknown";
    
    private const long SizeInFile = 16;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        CrcValue = stream.ReadUInt32();
        ConstLoc = stream.ReadStruct<ConstLocData>();
        Flags = stream.ReadUInt8();
        AddressU = stream.ReadUInt8();
        AddressV = stream.ReadUInt8();
        AddressW = stream.ReadUInt8();
        MinFilter = stream.ReadUInt8();
        MagFilter = stream.ReadUInt8();
        MipFilter = stream.ReadUInt8();
        ColorData = stream.ReadUInt8();
        stream.Skip(3);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TexFixup. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}