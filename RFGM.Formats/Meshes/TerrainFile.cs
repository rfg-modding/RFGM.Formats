using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Meshes.Terrain;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

//Contains low lod meshes and metadata for the terrain of one map zone. Extension = cterrain_pc|gterrain_pc.
public class TerrainFile
{
    public string Name;
    public bool LoadedCpuFile { get; private set; }
    
    public TerrainHeader Header;
    public List<string> TextureNames = new();
    public List<string> StitchPieceNames = new();
    public List<string> FmeshNames = new();
    public List<TerrainStitchInfo> StitchPieces = new();
    public TerrainData Data;

    public List<string> TerrainMaterialNames = new();
    public List<RfgMaterial> Materials = new();
    public List<SidemapMaterial> SidemapMaterials = new();
    public List<string> LayerMapMaterialNames = new();
    public List<string> LayerMapMaterialNames2 = new();
    
    public List<UndergrowthLayerData> UndergrowthLayers = new();
    public List<UndergrowthCellLayerData> UndergrowthCellData = new();
    public List<SingleUndergrowthCellLayerData> SingleUndergrowthCellData = new();
    public List<SingleUndergrowthData> SingleUndergrowthData = new();

    public List<string> MinimapMaterialNames = new();
    public RfgMaterial MinimapMaterials = new();

    public MeshConfig[] Meshes = new MeshConfig[9];
    
    private const uint HavokSignature = 1212891981;

    public TerrainFile(string name)
    {
        Name = name;
    }
    
    public void ReadHeader(Stream cpuFile)
    {
        Header.Read(cpuFile);
        if (Header.Signature != 1381123412) //ASCII string "TERR"
        {
            throw new Exception($"Unsupported terrain file signature. Expected 1381123412, found {Header.Signature}");
        }
        if (Header.Version != 31)
        {
            throw new Exception($"Unsupported terrain file version. Expected 31, found {Header.Version}");
        }

        TextureNames = cpuFile.ReadSizedStringList(Header.TextureNamesSize);
        cpuFile.AlignRead(4);
        
        StitchPieceNames = cpuFile.ReadSizedStringList(Header.StitchPieceNamesSize);
        for (int i = 0; i < Header.NumStitchPieces; i++)
        {
            TerrainStitchInfo stitchInfo = new();
            stitchInfo.Read(cpuFile);
            StitchPieces.Add(stitchInfo);
        }
        cpuFile.AlignRead(4);
        
        FmeshNames = cpuFile.ReadSizedStringList(Header.FmeshNamesSize);
        cpuFile.AlignRead(4);

        Data = new TerrainData();
        Data.Read(cpuFile);
        
        uint terrainMaterialsNameListSize = cpuFile.ReadUInt32();
        TerrainMaterialNames = cpuFile.ReadSizedStringList(terrainMaterialsNameListSize);
        cpuFile.AlignRead(16);
        
        //TODO: Fix this hack
        //Hack to get to the next known piece of data
        if (cpuFile.Peek<uint>() < 1000000)
        {
            cpuFile.Skip(4);
            cpuFile.AlignRead(16);
        }
        
        //TODO: Figure out if there's any important data in the skips
        cpuFile.Skip(4);
        uint numMaterials = cpuFile.ReadUInt32();
        cpuFile.Skip(28);
        cpuFile.Skip(numMaterials * 4);
        cpuFile.AlignRead(16);

        //TODO: Fix
        //Another hack like above
        if (cpuFile.Peek<uint>() == 0)
        {
            cpuFile.Skip(4);
            cpuFile.AlignRead(16);
        }

        for (int i = 0; i < numMaterials; i++)
        {
            RfgMaterial material = new();
            material.Read(cpuFile);
            Materials.Add(material);
        }

        if (Data.ShapeHandle != uint.MaxValue)
        {
            if (!SkipHavokData(cpuFile))
            {
                throw new Exception("Failed to skip havok data in terrain file");
            }
        }
        
        cpuFile.AlignRead(4);
        cpuFile.Skip(Data.NumSubzones * 4);
        if (Data.NumSidemapMaterials > 0)
        {
            cpuFile.Skip(8);
            cpuFile.Skip(Data.NumSidemapMaterials * 4 * 2);
            for (int i = 0; i < Data.NumSidemapMaterials; i++)
            {
                SidemapMaterial material = new();
                material.Read(cpuFile);
                SidemapMaterials.Add(material);
            }
        }
        
        //Appears to be navmesh/pathfinding data
        cpuFile.AlignRead(4);
        uint maybeNumNavmeshes = cpuFile.ReadUInt32();
        uint maybeNavmeshSize = cpuFile.ReadUInt32();
        cpuFile.Skip(maybeNavmeshSize - 4);
        cpuFile.AlignRead(16);

        if (cpuFile.Peek<uint>() == HavokSignature)
        {
            if (!SkipHavokData(cpuFile))
            {
                throw new Exception("Failed to skip havok data in terrain file");
            }
        }
        
        //Likely invisible barrier data
        cpuFile.AlignRead(4);
        cpuFile.Skip(Data.NumInvisibleBarriers * 8);
        cpuFile.AlignRead(16);
        
        if (cpuFile.Peek<uint>() == HavokSignature)
        {
            if (!SkipHavokData(cpuFile))
            {
                throw new Exception("Failed to skip havok data in terrain file");
            }
        }
        
        //Todo: Determine purpose, maybe related to undergrowth/grass placement
        //Layer map data. Seems to have BitDepth * ResX * ResY bits
        cpuFile.AlignRead(16);
        cpuFile.Skip(Data.LayerMap.DataSize);
        cpuFile.Skip(Data.LayerMap.NumMaterials * 4);
        for (int i = 0; i < Data.LayerMap.NumMaterials; i++)
        {
            LayerMapMaterialNames.Add(cpuFile.ReadAsciiNullTerminatedString());
        }
        cpuFile.AlignRead(4);
        cpuFile.Skip(Data.LayerMap.NumMaterials * 4);

        if (Data.NumUndergrowthLayers > 0)
        {
            //Undergrowth layer data
            for (int i = 0; i < Data.NumUndergrowthLayers; i++)
            {
                UndergrowthLayerData layerData = new();
                layerData.Read(cpuFile);
                UndergrowthLayers.Add(layerData);
            }

            int totalModels = 0;
            foreach (UndergrowthLayerData layer in UndergrowthLayers)
            {
                totalModels += layer.NumModels;
            }
            
            cpuFile.Skip(totalModels * 16);
            for (int i = 0; i < totalModels; i++)
            {
                LayerMapMaterialNames2.Add(cpuFile.ReadAsciiNullTerminatedString());
                cpuFile.AlignRead(4);
            }
            cpuFile.AlignRead(4);
            cpuFile.Skip(16384); //TODO: Figure out what this data is
            
            //Undergrowth cell data
            for (int i = 0; i < Data.NumUndergrowthCellLayerDatas; i++)
            {
                UndergrowthCellLayerData cellLayerData = new();
                cellLayerData.Read(cpuFile);
                UndergrowthCellData.Add(cellLayerData);
            }
            cpuFile.AlignRead(4);
            
            //More undergrowth data
            for (int i = 0; i < Data.NumUndergrowthCellLayerDatas; i++)
            {
                SingleUndergrowthCellLayerData cellLayerData = new();
                cellLayerData.Read(cpuFile);
                SingleUndergrowthCellData.Add(cellLayerData);
            }
            
            int numSingleUndergrowths = 0;
            foreach (SingleUndergrowthCellLayerData cell in SingleUndergrowthCellData)
            {
                numSingleUndergrowths += (int)cell.NumSingleUndergrowth;
            }
            for (int i = 0; i < numSingleUndergrowths; i++)
            {
                SingleUndergrowthData data = new();
                data.Read(cpuFile);
                SingleUndergrowthData.Add(data);
            }
            
            //TODO: Fix this
            //Another hack to get to the next data block
            cpuFile.Align(4);
            while (cpuFile.Peek<uint>() == 0 || cpuFile.Peek<uint>() > 1000 || cpuFile.Peek<uint>() < 5)
            {
                cpuFile.Skip(4);
            }
        }
        cpuFile.AlignRead(4);
        
        uint minimapMaterialNamesSize = cpuFile.ReadUInt32();
        MinimapMaterialNames = cpuFile.ReadSizedStringList(minimapMaterialNamesSize);
        cpuFile.AlignRead(16);

        MinimapMaterials.Read(cpuFile);
        cpuFile.Skip(432);
        
        //Read mesh info
        for (int i = 0; i < 9; i++)
        {
            Meshes[i] = new MeshConfig();
            Meshes[i].Read(cpuFile);
        }
        
        LoadedCpuFile = true;
    }

    private bool SkipHavokData(Stream stream)
    {
        long startPos = stream.Position;
        uint maybeSignature = stream.ReadUInt32();
        if (maybeSignature != HavokSignature)
            return false;
        
        stream.Skip(4);
        uint size = stream.ReadUInt32();
        stream.Seek(startPos + size, SeekOrigin.Begin);
        return true;
    }

    public MeshInstanceData ReadData(Stream gpuFile, int index)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call TerrainFile.ReadHeader() before calling TerrainFile.ReadData() on TerrainFile");
        }
        if (index < 0 || index > 8)
        {
            throw new Exception("Terrain index must be between 0 and 8 inclusive");            
        }

        //Calculate mesh data offset in gpu file
        long meshStartPos = 0;
        for (int i = 0; i < index; i++)
        {
            MeshConfig mesh = Meshes[i];
            meshStartPos += mesh.VerticesOffset + (mesh.NumVertices * mesh.VertexStride0);
            meshStartPos += 4; //Skip end CRC
        }

        MeshConfig config = Meshes[index];
        
        //Sanity check. Make sure CRCs match. If not something probably went wrong when reading/writing from packfile
        gpuFile.Seek(meshStartPos, SeekOrigin.Begin);
        uint startCRC = gpuFile.ReadUInt32();
        if (startCRC != config.VerificationHash)
        {
            throw new Exception($"Start CRC mismatch in gterrain_pc file {Name}");
        }
        
        //Read index buffer
        gpuFile.Seek(meshStartPos + config.IndicesOffset, SeekOrigin.Begin);
        uint indexBufferSize = config.NumIndices * config.IndexSize;
        byte[] indexBuffer = new byte[indexBufferSize];
        gpuFile.ReadExactly(indexBuffer);

        //Read vertex buffer
        gpuFile.Seek(meshStartPos + config.VerticesOffset, SeekOrigin.Begin);
        uint vertexBufferSize = config.NumVertices * config.VertexStride0;
        byte[] vertexBuffer = new byte[vertexBufferSize];
        gpuFile.ReadExactly(vertexBuffer);
        
        //Sanity check. CRC at the end of the mesh data should match the start
        uint endCRC = gpuFile.ReadUInt32();
        if (startCRC != endCRC)
        {
            throw new Exception($"End CRC mismatch in gterrain_pc file {Name}");
        }

        return new MeshInstanceData(config, vertexBuffer, indexBuffer);
    }
}