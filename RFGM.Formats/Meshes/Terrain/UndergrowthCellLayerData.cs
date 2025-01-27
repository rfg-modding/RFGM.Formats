using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

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
        var startPos = stream.Position;        
#endif
        
        LayerIndex = stream.ReadUInt8();
        Density = stream.ReadUInt8();
        var bitmaskBytesRead = stream.Read(Bitmask, 0, 8);
        if (bitmaskBytesRead != 8)
        {
            throw new Exception("Failed to read Bitmask for UndergrowthCellLayerData");
        }
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for UndergrowthCellLayerData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}