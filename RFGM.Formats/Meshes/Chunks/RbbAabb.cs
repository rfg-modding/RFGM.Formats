using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct RbbAabb
{
    public short MinX;
    public short MinY;
    public short MinZ;
    public short MaxX;
    public short MaxY;
    public short MaxZ;
    
    private const long SizeInFile = 12;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        MinX = stream.ReadInt16();
        MinY = stream.ReadInt16();
        MinZ = stream.ReadInt16();
        MaxX = stream.ReadInt16();
        MaxY = stream.ReadInt16();
        MaxZ = stream.ReadInt16();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RbbAabb. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}