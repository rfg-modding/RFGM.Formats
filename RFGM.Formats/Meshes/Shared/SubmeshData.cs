using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public struct SubmeshData
{
    public uint NumRenderBlocks;
    public Vector3 Offset;
    public Vector3 Bmin;
    public Vector3 Bmax;
    public uint RenderBlocksOffset;

    public void Read(Stream stream)
    {
        NumRenderBlocks = stream.ReadUInt32();
        Offset = stream.ReadStruct<Vector3>();
        Bmin = stream.ReadStruct<Vector3>();
        Bmax = stream.ReadStruct<Vector3>();
        RenderBlocksOffset = stream.ReadUInt32();
    }
}