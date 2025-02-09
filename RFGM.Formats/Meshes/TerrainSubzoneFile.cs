using RFGM.Formats.Materials;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Meshes.Terrain;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public class TerrainSubzoneFile(string name)
{
    public string Name = name;
    public bool LoadedCpuFile { get; private set; }

    //Header
    public uint Signature;
    public uint Version;
    public uint Index;
    public uint NumStitchPieceNames;

    //Terrain data
    public List<string> StitchPieceNames = new();
    public TerrainSubzoneData Data;
    public List<TerrainPatch> Patches = new();
    public MeshConfig TerrainMeshConfig = new();
    
    //Stitch piece data
    public List<TerrainStitchInstance> StitchInstances = new();
    public List<string> StitchPieceNames2 = new();
    public MeshConfig StitchMeshConfig = new();
    
    //Road data
    public List<RoadMeshData> RoadMeshesData = new();
    public List<MeshConfig> RoadMeshesConfig = new();
    public List<RfgMaterial> RoadMaterials = new();
    public List<List<string>> RoadTextures = new();

    public bool HasStitchMesh { get; private set; }
    public bool HasRoadMesh { get; private set; }
    
    public void ReadHeader(Stream cpuFile)
    {
        Signature = cpuFile.ReadUInt32();
        Version = cpuFile.ReadUInt32();
        Index = cpuFile.ReadUInt32();
        NumStitchPieceNames = cpuFile.ReadUInt32();
        if (Signature != 1514296659) //ASCII string "SUBZ"
        {
            throw new Exception($"Unsupported terrain subzone file signature. Expected 1381123412, found {Signature}");
        }
        if (Version != 31)
        {
            throw new Exception($"Unsupported terrain subzone file version. Expected 31, found {Version}");
        }

        var stitchPieceNamesSize = cpuFile.ReadUInt32();
        StitchPieceNames = cpuFile.ReadSizedStringList(stitchPieceNamesSize);
        cpuFile.AlignRead(4);
        
        Data.Read(cpuFile);

        for (var i = 0; i < Data.PatchCount; i++)
        {
            TerrainPatch patch = new();
            patch.Read(cpuFile);
            Patches.Add(patch);
        }
        cpuFile.AlignRead(16);
        
        TerrainMeshConfig.Read(cpuFile);
        cpuFile.AlignRead(4);
        
        //Read stitch piece data
        for (var i = 0; i < Data.NumStitchPieces; i++)
        {
            TerrainStitchInstance stitch = new();
            stitch.Read(cpuFile);
            StitchInstances.Add(stitch);
        }
        for (var i = 0; i < Data.NumStitchPieces; i++)
        {
            StitchPieceNames2.Add(cpuFile.ReadAsciiNullTerminatedString());
            cpuFile.AlignRead(4);
            cpuFile.Skip(4);
            
            //Todo: Fix this stupid hack
            //Skip unknown data that's between some strings
            if (i < Data.NumStitchPieces - 1)
            {
                while (cpuFile.Peek<byte>() < 33 || cpuFile.Peek<byte>() > 126)
                {
                    cpuFile.Skip(4);
                }
            }
        }
        cpuFile.Skip(4);
        
        //Read stitch mesh data
        if (Data.NumRoadDecalMeshes > 0)
        {
            HasStitchMesh = true;
            
            //TODO: Come up with a less hacky way of doing this
            //Skip unknown data before stitch mesh header that has indices which can be parsed
            var i = cpuFile.ReadUInt32();
            while (true)
            {
                if (cpuFile.Peek<byte>() == 0)
                {
                    cpuFile.Skip(4);
                }
                else if (cpuFile.Peek<byte>() == i + 1)
                {
                    cpuFile.Skip(4);
                    i++;

                    //Hit version at start of mesh data block, stop
                    if (cpuFile.Peek<byte>() != 0)
                    {
                        cpuFile.Seek(cpuFile.Position - 4, SeekOrigin.Begin);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            cpuFile.AlignRead(16);
            StitchMeshConfig.Read(cpuFile);
        }
        cpuFile.AlignRead(4);
        
        //Read road mesh data
        if (Data.NumRoadDecalMeshes > 0)
        {
            HasRoadMesh = true;
            for (var i = 0; i < Data.NumRoadDecalMeshes; i++)
            {
                RoadMeshData roadMesh = new();
                roadMesh.Read(cpuFile);
                RoadMeshesData.Add(roadMesh);
            }

            for (var i = 0; i < Data.NumRoadDecalMeshes; i++)
            {
                MeshConfig roadMeshConfig = new();

                cpuFile.AlignRead(16);
                roadMeshConfig.Read(cpuFile);
                cpuFile.AlignRead(4);
                
                //TODO: Fix this hack
                //Skip null data of varying size
                while (cpuFile.Peek<uint>() == 0)
                {
                    cpuFile.Skip(4);
                }

                var textureNamesSize = cpuFile.ReadUInt32();
                List<string> textureNames = cpuFile.ReadSizedStringList(textureNamesSize);
                RoadTextures.Add(textureNames);
                
                cpuFile.AlignRead(16);
                cpuFile.Skip(16);
                cpuFile.AlignRead(16);
                
                RfgMaterial roadMaterial = new();
                roadMaterial.Read(cpuFile);
                RoadMaterials.Add(roadMaterial);
            }
        }
        
        LoadedCpuFile = true;
    }

    public MeshInstanceData ReadTerrainMeshData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call TerrainSubzoneFile.ReadHeader() before calling TerrainSubzoneFile.ReadData() on TerrainSubzoneFile");
        }

        var startCRC = gpuFile.ReadUInt32();
        if (startCRC != TerrainMeshConfig.VerificationHash)
        {
            throw new Exception($"Start CRC mismatch while reading terrain mesh in gtmesh_pc file {Name}");
        }
        
        //Read indices
        gpuFile.Seek(TerrainMeshConfig.IndicesOffset, SeekOrigin.Begin);
        var indices = new byte[TerrainMeshConfig.NumIndices * TerrainMeshConfig.IndexSize];
        gpuFile.ReadExactly(indices);
        
        //Read vertices
        gpuFile.Seek(TerrainMeshConfig.VerticesOffset, SeekOrigin.Begin);
        var vertices = new byte[TerrainMeshConfig.NumVertices * TerrainMeshConfig.VertexStride0];
        gpuFile.ReadExactly(vertices);
        
        //Sanity check. CRC at the end of the mesh data should match the start
        var endCRC = gpuFile.ReadUInt32();
        if (startCRC != endCRC)
        {
            throw new Exception($"End CRC mismatch while reading terrain mesh in gtmesh_pc file {Name}");
        }
        
        return new MeshInstanceData(TerrainMeshConfig, vertices, indices);
    }
    
    public MeshInstanceData ReadStitchMeshData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call TerrainSubzoneFile.ReadHeader() before calling TerrainSubzoneFile.ReadStitchMeshData() on TerrainSubzoneFile");
        }
        if (!HasStitchMesh)
        {
            throw new Exception("This terrain subzone has no stitch meshes.");
        }
        
        //Skip terrain mesh data
        gpuFile.Seek(TerrainMeshConfig.VerticesOffset, SeekOrigin.Begin);
        gpuFile.Skip(TerrainMeshConfig.NumVertices * TerrainMeshConfig.VertexStride0);
        gpuFile.Skip(4); //Skip verification hash
        
        //Start of stitch mesh
        gpuFile.AlignRead(16);
        var startPos = gpuFile.Position;
        var startCRC = gpuFile.ReadUInt32();
        if (startCRC != StitchMeshConfig.VerificationHash)
        {
            throw new Exception($"Start CRC mismatch while reading stitch mesh in gtmesh_pc file {Name}");
        }
        
        //Read indices
        gpuFile.Seek(startPos + StitchMeshConfig.IndicesOffset, SeekOrigin.Begin);
        var indices = new byte[StitchMeshConfig.NumIndices * StitchMeshConfig.IndexSize];
        gpuFile.ReadExactly(indices);
        
        //Read vertices
        gpuFile.Seek(startPos + StitchMeshConfig.VerticesOffset, SeekOrigin.Begin);
        var vertices = new byte[StitchMeshConfig.NumVertices * StitchMeshConfig.VertexStride0];
        gpuFile.ReadExactly(vertices);

        var endCRC = gpuFile.ReadUInt32();
        if (startCRC != endCRC)
        {
            throw new Exception($"End CRC mismatch while reading stitch mesh in gtmesh_pc file {Name}");
        }
        
        return new MeshInstanceData(StitchMeshConfig, vertices, indices);
    }
    
    public List<MeshInstanceData> ReadRoadMeshData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call TerrainSubzoneFile.ReadHeader() before calling TerrainSubzoneFile.ReadRoadMeshData() on TerrainSubzoneFile");
        }
        if (!HasRoadMesh)
        {
            throw new Exception("This terrain subzone has no road meshes.");
        }
        
        //Skip terrain mesh data
        gpuFile.Seek(TerrainMeshConfig.VerticesOffset, SeekOrigin.Begin);
        gpuFile.Skip(TerrainMeshConfig.NumVertices * TerrainMeshConfig.VertexStride0);
        gpuFile.Skip(4); //Skip verification hash
        
        //Skip stitch mesh data
        gpuFile.AlignRead(16);
        var stitchStartPos = gpuFile.Position;
        gpuFile.Seek(stitchStartPos + StitchMeshConfig.VerticesOffset, SeekOrigin.Begin);
        gpuFile.Skip(StitchMeshConfig.NumVertices * StitchMeshConfig.VertexStride0);
        gpuFile.Skip(4); //Skip verification hash
        
        //Start of road mesh data
        List<MeshInstanceData> meshes = new List<MeshInstanceData>();
        for (var i = 0; i < RoadMeshesConfig.Count; i++)
        {
            var config = RoadMeshesConfig[i];
            gpuFile.AlignRead(16);
            var startPos = gpuFile.Position;
            var startCRC = gpuFile.ReadUInt32();
            if (startCRC != config.VerificationHash)
            {
                throw new Exception($"Start CRC mismatch while reading road mesh {i} in gtmesh_pc file {Name}");
            }

            //Read index buffer
            gpuFile.AlignRead(16);
            gpuFile.Seek(startPos + config.IndicesOffset, SeekOrigin.Begin);
            var indices = new byte[config.NumIndices * config.IndexSize];
            gpuFile.ReadExactly(indices);

            //Read vertex buffer
            gpuFile.Seek(startPos + config.VerticesOffset, SeekOrigin.Begin);
            var vertices = new byte[config.NumVertices * config.VertexStride0];
            gpuFile.ReadExactly(vertices);

            var endCRC = gpuFile.ReadUInt32();
            if (startCRC != endCRC)
            {
                throw new Exception($"End CRC mismatch while reading road mesh {i} in gtmesh_pc file {Name}");
            }
            
            meshes.Add(new MeshInstanceData(config, vertices, indices));
        }
        
        return meshes;
    }
}