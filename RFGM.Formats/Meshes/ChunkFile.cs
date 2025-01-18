using RFGM.Formats.Meshes.Chunks;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

//Chunk mesh. Buildings and other destructible objects are stored in this format. Extension = cchk_pc|gchk_pc
public class ChunkFile(string name)
{
    public string Name = name;
    public bool LoadedCpuFile { get; private set; } = false;

    public ChunkFileHeader Header;

    public MeshConfig Config = new();
    public List<string> Textures = new();
    public List<Destroyable> Destroyables = new();
    
    private const uint HavokSignature = 1212891981;

    //Note: This currently doesn't work for all chunk files. Need to do further reverse engineering and figure out what the problem is with this code + replace any hacky code.
    //TODO: Make this work for all chunk files - See line 129
    public void ReadHeader(Stream cpuFile)
    {
        //Validate header
        Header = cpuFile.ReadStruct<ChunkFileHeader>();
        if (Header.Signature != 2966351781)
        {
            throw new Exception($"Invalid chunk file signature. Expected 2966351781, found {Header.Signature}.");
        }
        if (Header.Version != 56)
        {
            throw new Exception($"Invalid chunk file version. Expected 56, found {Header.Version}.");
        }
        if (Header.SourceVersion != 20)
        {
            throw new Exception($"Invalid chunk file source version. Expected 20, found {Header.SourceVersion}.");
        }
        
        //Skip some unknown data. Skipped/read in the current best guess for how the data is structured
        cpuFile.Skip(400);
        uint unkValue0 = cpuFile.ReadUInt32();
        cpuFile.Skip(28);
        uint unkValue1 = cpuFile.ReadUInt32();
        cpuFile.Skip(32);
        uint unkValue2 = cpuFile.ReadUInt32();
        cpuFile.Skip(184);
        
        //Should be at the render data offset
        if (cpuFile.Position != Header.RenderCpuDataOffset)
        {
            throw new Exception($"Error! Haven't reached the chunk render data section of chunk file {Name} when expected!");
        }
        
        Config.Read(cpuFile);
        
        //Read texture names
        cpuFile.AlignRead(16);
        uint textureNamesBlockSize = cpuFile.ReadUInt32();
        Textures = cpuFile.ReadSizedStringList(textureNamesBlockSize);

        //TODO: Figure out what this data is
        //Some kind of material data
        cpuFile.AlignRead(16);
        uint materialOffset = cpuFile.ReadUInt32();
        uint numMaterials = cpuFile.ReadUInt32();
        cpuFile.Skip(numMaterials * 4); //Potentially a list of material IDs or offsets
        cpuFile.Skip(materialOffset);
        cpuFile.Skip(numMaterials * 8);
        cpuFile.AlignRead(16);

        //TODO: Figure out what data is between here and the destroyables list
        
        //Skip to destroyables. Haven't fully reversed the format yet
        cpuFile.Seek(Header.DestructionOffset, SeekOrigin.Begin);
        cpuFile.AlignRead(128);
        uint numDestroyables = cpuFile.ReadUInt32();
        cpuFile.Skip((numDestroyables * 8) + 4);
        cpuFile.AlignRead(16);
        
        //Read destroyables
        for (int i = 0; i < numDestroyables; i++)
        {
            Destroyable destroyable = new();
            long destroyableStartPos = cpuFile.Position;
            
            destroyable.Header = cpuFile.ReadStruct<DestroyableHeader>();
            cpuFile.AlignRead(128);

            for (int j = 0; j < destroyable.Header.NumObjects; j++)
            {
                Subpiece subpiece = new();
                subpiece.Read(cpuFile);
                destroyable.Subpieces.Add(subpiece);
            }
            for (int j = 0; j < destroyable.Header.NumObjects; j++)
            {
                SubpieceData subpieceData = new();
                subpieceData.Read(cpuFile);
                destroyable.SubpieceData.Add(subpieceData);
            }
            
            //TODO: Figure out what this data is meant to be. Game has some physical material code here. Maybe link material
            foreach (Subpiece subpiece in destroyable.Subpieces)
            {
                cpuFile.Skip(subpiece.NumLinks * 2);
            }
            
            cpuFile.AlignRead(4);
            
            //Read links
            for (int j = 0; j < destroyable.Header.NumLinks; j++)
            {
                Link link = new();
                link.Read(cpuFile);
                destroyable.Links.Add(link);
            }
            cpuFile.AlignRead(4);
            
            //Read dlods
            for (int j = 0; j < destroyable.Header.NumDlods; j++)
            {
                Dlod dlod = new();
                dlod.Read(cpuFile);
                destroyable.Dlods.Add(dlod);
            }
            cpuFile.AlignRead(4);
            
            //TODO: REQUIRED FOR THIS TO WORK
            //TODO: Figure out what data goes here and write code to reliably load it
        }
        
        //Skip collision models. Some kind of havok data format that hasn't been reversed yet.
        if (!SkipHavokData(cpuFile))
        {
            throw new Exception("Failed to skip havok data in chunk file.");
        }
        
        //Read destroyable UIDs, names, indices
        cpuFile.AlignRead(128);
        for (int i = 0; i < Destroyables.Count; i++)
        {
            uint uid = cpuFile.ReadUInt32();
            
            //TODO: Move this into a helper function and use it in RigFileHeader too
            //Read destroyable name which is up to 24 characters long
            string destroyableName = string.Empty;
            for (int j = 0; j < 24; j++)
            {
                if (cpuFile.Peek<char>() == '\0')
                    break;
            
                destroyableName += cpuFile.ReadChar8();
            }
            
            int destroyableIndex = cpuFile.ReadInt32();
            uint isDestroyable = cpuFile.ReadUInt32();
            uint numSnapPoints = cpuFile.ReadUInt32();
            
            Destroyable destroyable = Destroyables[destroyableIndex];
            destroyable.UID = uid;
            destroyable.Name = destroyableName;
            destroyable.IsDestroyable = isDestroyable;
            destroyable.NumSnapPoints = numSnapPoints;
            for (int j = 0; j < 10; j++)
            {
                destroyable.SnapPoints[j].Read(cpuFile);
            }
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
    
    public MeshInstanceData ReadData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call ChunkFile.ReadHeader() before calling ChunkFile.ReadData() on ChunkFile");
        }
        
        //Read index buffer
        gpuFile.Seek(16, SeekOrigin.Begin);
        byte[] indices = new byte[Config.NumIndices * Config.IndexSize];
        gpuFile.ReadExactly(indices);
        
        //Read vertex buffer
        gpuFile.AlignRead(16);
        byte[] vertices = new byte[Config.NumVertices * Config.VertexStride0];
        gpuFile.ReadExactly(vertices);
        
        return new MeshInstanceData(Config, vertices, indices);
    }
}