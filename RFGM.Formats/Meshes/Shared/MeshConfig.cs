using RFGM.Formats.Streams;
using Silk.NET.Core.Native;

namespace RFGM.Formats.Meshes.Shared;

//Config block thats used by most of the mesh formats. In formats that contain multiple meshes like [c|g]terrain_pc files there will be one of these per mesh.
//Note: The order and size of each field is intentional. They match the data layout in the files.
//Note: This class was renamed from MeshDataBlock during the C# port.
public class MeshConfig
{
    //Header
    public uint Version;
    public uint VerificationHash;
    public uint CpuDataSize;
    public uint GpuDataSize;
    public uint NumSubmeshes;
    public uint SubmeshesOffset;
    
    //Vertex layout
    public uint NumVertices;
    public byte VertexStride0;
    public VertexFormat VertexFormat;
    public byte NumUvChannels;
    public byte VertexStride1;
    public uint VerticesOffset;

    //Index layout
    public uint NumIndices;
    public uint IndicesOffset;
    public byte IndexSize;
    public PrimitiveTopology Topology;
    public ushort NumRenderBlocks;

    public List<SubmeshData> Submeshes = new();
    public List<RenderBlock> RenderBlocks = new();

    public void Read(Stream stream, bool patchBufferOffsets = false)
    {
        long startPos = stream.Position;
        
        Version = stream.ReadUInt32();
        VerificationHash = stream.ReadUInt32();
        CpuDataSize = stream.ReadUInt32();
        GpuDataSize = stream.ReadUInt32();
        NumSubmeshes = stream.ReadUInt32();
        SubmeshesOffset = stream.ReadUInt32();
        
        NumVertices = stream.ReadUInt32();
        VertexStride0 = stream.ReadUInt8();
        VertexFormat = (VertexFormat)stream.ReadUInt8();
        NumUvChannels = stream.ReadUInt8();
        VertexStride1 = stream.ReadUInt8();
        VerticesOffset = stream.ReadUInt32();
        
        NumIndices = stream.ReadUInt32();
        IndicesOffset = stream.ReadUInt32();
        IndexSize = stream.ReadUInt8();
        Topology = (PrimitiveTopology)stream.ReadUInt8();
        NumRenderBlocks = stream.ReadUInt16();

        stream.AlignRead(16);
        for (int i = 0; i < NumSubmeshes; i++)
        {
            SubmeshData submesh = new();
            submesh.Read(stream);
            Submeshes.Add(submesh);
        }

        uint numRenderBlocks = 0;
        foreach (var submesh in Submeshes)
        {
            numRenderBlocks += submesh.NumRenderBlocks;
        }
        for (int i = 0; i < numRenderBlocks; i++)
        {
            RenderBlock renderBlock = new();
            renderBlock.Read(stream);
            RenderBlocks.Add(renderBlock);
        }
        
        //TODO: Fix this for files like gterrain_pc and gtmesh_pc that have multiple meshes. Seems to need absolute offset to calculate correct align pad. Luckily they have correct offsets by default
        //Patch vertex and index offset since some files don't have correct values.
        if (patchBufferOffsets)
        {
            IndicesOffset = 16;
            uint indicesEnd = IndicesOffset + (NumIndices * IndexSize);
            VerticesOffset = indicesEnd + (uint)stream.CalcAlignment(indicesEnd, 16);
        }
        
        //Patch render block offsets for easy access later
        uint renderBlockOffset = 0;
        for (int i = 0; i < Submeshes.Count; i++)
        {
            SubmeshData submesh = Submeshes[i];
            submesh.RenderBlocksOffset = renderBlockOffset;
            renderBlockOffset += submesh.NumRenderBlocks;
        }
        
        uint endVerificationHash = stream.ReadUInt32();
        if (VerificationHash != endVerificationHash)
            throw new Exception("MeshConfig begin/end verification hash mismatch");
        if (stream.Position - startPos != CpuDataSize)
            throw new Exception("MeshConfig isn't the expected size");
    }
}