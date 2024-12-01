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
        NumRenderBlocks = stream.Read<uint>();
        Offset = stream.Read<Vector3>();
        Bmin = stream.Read<Vector3>();
        Bmax = stream.Read<Vector3>();
        RenderBlocksOffset = stream.Read<uint>();
    }
}