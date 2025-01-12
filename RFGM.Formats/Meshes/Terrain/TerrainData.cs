using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public struct TerrainData
{
    public Vector3 Bmin;
    public Vector3 Bmax;
    public uint Xres;
    public uint Zres;
    public uint NumOccluders;
    public uint OccludersOffset;
    public uint TerrainMaterialMapOffset;
    public uint TerrainMaterialsOffset;
    public uint NumTerrainMaterials;
    public uint MinimapMaterialHandle;
    public uint MinimapMaterialOffset;
    public uint LowLodPatchesOffset;
    public uint LowLodMaterialOffset;
    public uint LowLodMaterialMapOffset;
    public uint NumSubzones;
    public uint SubzonesOffset;
    public uint PfDataOffset;
    public TerrainLayerMap LayerMap;
    public uint NumUndergrowthLayers;
    public uint UndergrowthLayersOffset;
    public uint UndergrowthCellDataOffset;
    public uint NumUndergrowthCellLayerDatas;
    public uint UndergrowthCellLayerDataOffset;
    public uint SingleUndergrowthCellLayerDataOffset;
    public uint StitchPieceCmIndex;
    public uint NumInvisibleBarriers;
    public uint InvisibleBarriersOffset;
    public uint ShapeHandle;
    public uint NumSidemapMaterials;
    public uint SidemapDataOffset;
    public uint ObjectStubOffset;
    public uint StitchPhysicsInstancesOffset;
    public uint NumStitchPhysicsInstances;
    public uint ObjectStubPtr;
    public uint ObjectStubPtrPadding;
    //880 bytes of padding

    private const long SizeInFile = 1064;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        Bmin = stream.ReadStruct<Vector3>();
        Bmax = stream.ReadStruct<Vector3>();
        Xres = stream.ReadUInt32();
        Zres = stream.ReadUInt32();
        NumOccluders = stream.ReadUInt32();
        OccludersOffset = stream.ReadUInt32();
        TerrainMaterialMapOffset = stream.ReadUInt32();
        TerrainMaterialsOffset = stream.ReadUInt32();
        NumTerrainMaterials = stream.ReadUInt32();
        MinimapMaterialHandle = stream.ReadUInt32();
        MinimapMaterialOffset = stream.ReadUInt32();
        LowLodPatchesOffset = stream.ReadUInt32();
        LowLodMaterialOffset = stream.ReadUInt32();
        LowLodMaterialMapOffset = stream.ReadUInt32();
        NumSubzones = stream.ReadUInt32();
        SubzonesOffset = stream.ReadUInt32();
        PfDataOffset = stream.ReadUInt32();
        LayerMap = new TerrainLayerMap();
        LayerMap.Read(stream);
        NumUndergrowthLayers = stream.ReadUInt32();
        UndergrowthLayersOffset = stream.ReadUInt32();
        UndergrowthCellDataOffset = stream.ReadUInt32();
        NumUndergrowthCellLayerDatas = stream.ReadUInt32();
        UndergrowthCellLayerDataOffset = stream.ReadUInt32();
        SingleUndergrowthCellLayerDataOffset = stream.ReadUInt32();
        StitchPieceCmIndex = stream.ReadUInt32();
        NumInvisibleBarriers = stream.ReadUInt32();
        InvisibleBarriersOffset = stream.ReadUInt32();
        ShapeHandle = stream.ReadUInt32();
        NumSidemapMaterials = stream.ReadUInt32();
        SidemapDataOffset = stream.ReadUInt32();
        ObjectStubOffset = stream.ReadUInt32();
        StitchPhysicsInstancesOffset = stream.ReadUInt32();
        NumStitchPhysicsInstances = stream.ReadUInt32();
        ObjectStubPtr = stream.ReadUInt32();
        ObjectStubPtrPadding = stream.ReadUInt32();
        stream.Skip(880);        
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}