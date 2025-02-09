using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public struct VfxFloatKey
{
    public float Time;
    public float Value;
    
    private const long SizeInFile = 8;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        Time = stream.ReadFloat();
        Value = stream.ReadFloat();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxFloatKey. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}