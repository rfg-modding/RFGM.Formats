using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public class ObjectSkirt
{
    public ushort RenderMeshIndex;
    public int NumWeldEdges;
    public uint WeldEdgesOffset;
    
    //Note: This is stored after all of the skirts in the .cchk_pc, so we can't read them in ObjectSkirt.Read()
    public List<ObjectSkirtEdge> Edges = new();
    
    private const long SizeInFile = 16;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        RenderMeshIndex = stream.ReadUInt16();
        stream.Skip(2); //Padding
        NumWeldEdges = stream.ReadInt32();
        WeldEdgesOffset = stream.ReadUInt32();
        stream.Skip(4);
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ObjectSkirt. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}