using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class EffectFileHeader
{
    public uint Signature;
    public uint Version;
    public int NumExpressions;
    public uint ExpressionsOffset;
    public uint NameOffset;
    public float Duration;
    public float Radius;
    public int NumBitmaps;
    public uint BitmapNamesOffset;
    public uint MeshBitmapsOffset;
    public int NumMeshes;
    public uint MeshesOffset;
    public int NumEmitters;
    public uint EmittersOffset;
    public int NumLights;
    public uint LightsOffset;
    public int NumFilters;
    public uint FiltersOffset;
    public int NumCoronas;
    public uint CoronasOffset;
    
    private const long SizeInFile = 144;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        Signature = stream.ReadUInt32();
        Version = stream.ReadUInt32();
        NumExpressions = stream.ReadInt32();
        stream.Skip(4); //Padding
        
        ExpressionsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NameOffset = stream.ReadUInt32();
        stream.Skip(4);

        Duration = stream.ReadFloat();
        Radius = stream.ReadFloat();
        NumBitmaps = stream.ReadInt32();
        stream.Skip(4); //Padding
        
        BitmapNamesOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        MeshBitmapsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumMeshes = stream.ReadInt32();
        stream.Skip(4); //Padding

        MeshesOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumEmitters = stream.ReadInt32();
        stream.Skip(4); //Padding

        EmittersOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLights = stream.ReadInt32();
        stream.Skip(4); //Padding

        LightsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumFilters = stream.ReadInt32();
        stream.Skip(4); //Padding

        FiltersOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumCoronas = stream.ReadInt32();
        stream.Skip(4); //Padding

        CoronasOffset = stream.ReadUInt32();
        stream.Skip(4);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for EffectFileHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}