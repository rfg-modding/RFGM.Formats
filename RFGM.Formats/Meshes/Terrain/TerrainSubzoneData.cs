using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct TerrainSubzoneData
{
    public uint SubzoneIndex;
    public Vector3 Position;
    public uint PatchCount;
    public uint PatchesOffset;
    public TerrainRenderableData RenderableData;
    public uint NumDecals;
    public uint DecalsOffset;
    public uint StitchMeshDataOffset;
    public uint StitchRenderableOffset;
    public uint NumStitchPieces;
    public uint StitchPiecesOffset;
    public uint NumRoadDecalMeshes;
    public uint RoadDecalMeshesOffset;
    public uint HeaderVersion;
    //996 bytes padding
    
    private const long SizeInFile = 1104;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        SubzoneIndex = stream.ReadUInt32();
        Position = stream.ReadStruct<Vector3>();
        PatchCount = stream.ReadUInt32();
        PatchesOffset = stream.ReadUInt32();
        RenderableData.Read(stream);
        NumDecals = stream.ReadUInt32();
        DecalsOffset = stream.ReadUInt32();
        StitchMeshDataOffset = stream.ReadUInt32();
        StitchRenderableOffset = stream.ReadUInt32();
        NumStitchPieces = stream.ReadUInt32();
        StitchPiecesOffset = stream.ReadUInt32();
        NumRoadDecalMeshes = stream.ReadUInt32();
        RoadDecalMeshesOffset = stream.ReadUInt32();
        HeaderVersion = stream.ReadUInt32();
        stream.Skip(996);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainSubzoneData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}