using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Shaders;

public struct ShaderTechnique
{
    public short VertexShaderIndex;
    public short PixelShaderIndex;
    public uint TechniqueNameIndex;
    public uint Flags;

    public string TechniqueName => HashDictionary.FindOriginString(TechniqueNameIndex) ?? "Unknown";
    
    private const long SizeInFile = 12;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        VertexShaderIndex = stream.ReadInt16();
        PixelShaderIndex = stream.ReadInt16();
        TechniqueNameIndex = stream.ReadUInt32();
        Flags = stream.ReadUInt32();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ShaderTechnique. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}