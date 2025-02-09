using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public struct VfxQuaternionKey
{
    public float Time;
    public Quaternion Value;
    
    private const long SizeInFile = 20;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        Time = stream.ReadFloat();
        Value = stream.ReadStruct<Quaternion>();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxQuaternionKey. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}