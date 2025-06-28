using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public struct VfxVectorKey
{
    public float Time;
    public Vector3 Value;
    
    private const long SizeInFile = 16;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        Time = stream.ReadFloat();
        Value = stream.ReadStruct<Vector3>();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxVectorKey. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}