using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct UndergrowthLayerData
{
    public int NumModels;
    public uint ModelsOffset;
    public float MaxDensity;
    public float MaxFadeDistance;
    public float MinFadeDistance;
    public int PlacementMethod;
    public int RandomSeed;
    
    private const long SizeInFile = 28;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        NumModels = stream.ReadInt32();
        ModelsOffset = stream.ReadUInt32();
        MaxDensity = stream.ReadFloat();
        MaxFadeDistance = stream.ReadFloat();
        MinFadeDistance = stream.ReadFloat();
        PlacementMethod = stream.ReadInt32();
        RandomSeed = stream.ReadInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for UndergrowthLayerData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}