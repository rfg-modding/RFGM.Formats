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
        MaterialMapIndex = stream.ReadUInt16();
        stream.Skip(2);
        StartIndex = stream.ReadUInt32();
        NumIndices = stream.ReadUInt32();
        MinIndex = stream.ReadUInt32();
        MaxIndex = stream.ReadUInt32();
    }
}