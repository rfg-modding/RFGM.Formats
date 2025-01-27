using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct SingleUndergrowthCellLayerData
{
    public uint NumSingleUndergrowth;
    public uint NumExtraModelsOffset;
    public uint SingleUndergrowthOffset;
    
    private const long SizeInFile = 12;

    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif
        
        NumSingleUndergrowth = stream.ReadUInt32();
        NumExtraModelsOffset = stream.ReadUInt32();
        SingleUndergrowthOffset = stream.ReadUInt32();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for SingleUndergrowthCellLayerData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}