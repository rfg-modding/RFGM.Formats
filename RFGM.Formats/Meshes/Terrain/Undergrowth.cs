using System.Numerics;
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

public struct UndergrowthCellLayerData
{
    public byte LayerIndex = 0;
    public byte Density = 0;
    public byte[] Bitmask = new byte[8];

    private const long SizeInFile = 10;
    
    public UndergrowthCellLayerData()
    {
        
    }

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        LayerIndex = stream.ReadUInt8();
        Density = stream.ReadUInt8();
        int bitmaskBytesRead = stream.Read(Bitmask, 0, 8);
        if (bitmaskBytesRead != 8)
        {
            throw new Exception("Failed to read Bitmask for UndergrowthCellLayerData");
        }
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for UndergrowthCellLayerData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct SingleUndergrowthCellLayerData
{
    public uint NumSingleUndergrowth;
    public uint NumExtraModelsOffset;
    public uint SingleUndergrowthOffset;
    
    private const long SizeInFile = 12;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        NumSingleUndergrowth = stream.ReadUInt32();
        NumExtraModelsOffset = stream.ReadUInt32();
        SingleUndergrowthOffset = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for SingleUndergrowthCellLayerData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct SingleUndergrowthData
{
    public uint MeshIndex;
    public Vector3 Position;
    public float Scale;
    public float ColorLerp;
    
    private const long SizeInFile = 24;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
      
        MeshIndex = stream.ReadUInt32();
        Position = stream.ReadStruct<Vector3>();
        Scale = stream.ReadFloat();
        ColorLerp = stream.ReadFloat();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for SingleUndergrowthData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}