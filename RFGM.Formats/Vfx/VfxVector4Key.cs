using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public struct VfxVector4Key
{
    public float Time;
    public Vector4 Value;
    
    private const long SizeInFile = 32;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        Time = stream.ReadFloat();
        stream.Skip(12); //Padding
        Value = stream.ReadStruct<Vector4>();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxVector4Key. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}