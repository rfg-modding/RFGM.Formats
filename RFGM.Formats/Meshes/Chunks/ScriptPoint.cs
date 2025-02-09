using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public class ScriptPoint
{
    public Matrix4x3 Transform;
    public string Name;
    public string SubpieceName;
    public uint DestroyableIndex;
    
    private const long SizeInFile = 116;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        Transform = stream.ReadStruct<Matrix4x3>();
        Name = stream.ReadSizedString(32);
        SubpieceName = stream.ReadSizedString(32);
        DestroyableIndex = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ScriptPoint. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}