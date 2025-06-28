using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class ParticleConstantInfo
{
    public int ConstantRefs;
    public ushort NumConstantRefs;
    public ushort NumEngineConstants;
    public int EngineConstantsOffset;
    
    private const long SizeInFile = 12;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        ConstantRefs = stream.ReadInt32();
        NumConstantRefs = stream.ReadUInt16();
        NumEngineConstants = stream.ReadUInt16();
        EngineConstantsOffset = stream.ReadInt32();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ParticleConstantInfo. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}