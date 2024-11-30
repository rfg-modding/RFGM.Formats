using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public struct RenderBlock
{
    public ushort MaterialMapIndex;
    public uint StartIndex;
    public uint NumIndices;
    public uint MinIndex;
    public uint MaxIndex;

    public void Read(Stream stream)
    {
        MaterialMapIndex = stream.Read<ushort>();
        StartIndex = stream.Read<uint>();
        NumIndices = stream.Read<uint>();
        MinIndex = stream.Read<uint>();
        MaxIndex = stream.Read<uint>();
    }
}