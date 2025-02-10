using RFGM.Formats.Materials;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

//RFGR static mesh format. Extension: csmesh_pc|gsmesh_pc
public class StaticMesh(string filename)
{
    public string Filename => filename;
    
    public StaticMeshHeader Header;
    public MeshConfig Config = new();
    public List<uint> MaterialOffsets = new();
    public List<RfgMaterial> Materials = new();
    public List<string> TextureNames = new();
    public List<int> LodSubmeshIds = new();
    public List<MeshTag> Tags = new();

    public bool LoadedCpuFile { get; private set; }
    
    public struct StaticMeshHeader
    {
        public MeshHeaderShared Shared;
        
        public uint NumLods;
        //4 bytes padding
        public int LodSubmeshIdOffset;
        //4 bytes padding
        public int TagsOffset;
        //4 bytes padding
        public uint NumTags;
        //4 bytes padding
        public uint MeshTagOffset;
        //4 bytes padding
        public uint CmIndex;
        //4 bytes padding
    }

    public void ReadHeader(Stream cpuFile)
    {
        Header.Shared.Read(cpuFile);
        Header.NumLods = cpuFile.ReadUInt32();
        cpuFile.Skip(4);
        Header.LodSubmeshIdOffset = cpuFile.ReadInt32();
        cpuFile.Skip(4);
        Header.TagsOffset = cpuFile.ReadInt32();
        cpuFile.Skip(4);
        Header.NumTags = cpuFile.ReadUInt32();
        cpuFile.Skip(4);
        Header.MeshTagOffset = cpuFile.ReadUInt32();
        cpuFile.Skip(4);
        Header.CmIndex = cpuFile.ReadUInt32();
        cpuFile.Skip(4);
        
        if (Header.Shared.Signature != 0xC0FFEE11)
            throw new Exception("Invalid static mesh file signature detected. Expected 0xC0FFEE11.");
        if (Header.Shared.Version != 5)
            throw new Exception("Invalid static mesh file version detected. Expected 5.");
        
        //TODO: Double check that these seeks are from the beginning of the stream
        //TODO: Align(16) may be needed here
        cpuFile.Seek(Header.Shared.MeshOffset, SeekOrigin.Begin);
        Config.Read(cpuFile, patchBufferOffsets: true);
        
        //TODO: Align(16) may be needed here when an exporter is written.
        cpuFile.Seek(Header.Shared.MaterialMapOffset, SeekOrigin.Begin);
        
        //TODO: Determine if any other important data is between this and the material offsets. The null bytes might just be padding.
        var materialsOffsetRelative = cpuFile.ReadUInt32();
        var numMaterials = cpuFile.ReadUInt32();
        cpuFile.Seek(Header.Shared.MaterialsOffset, SeekOrigin.Begin);

        for (var i = 0; i < numMaterials; i++)
        {
            MaterialOffsets.Add(cpuFile.ReadUInt32());
            cpuFile.Skip(4);
        }

        for (var i = 0; i < numMaterials; i++)
        {
            //TODO: Make sure we're not skipping any important data by doing this
            cpuFile.Seek(MaterialOffsets[i], SeekOrigin.Begin);
            
            RfgMaterial material = new();
            material.Read(cpuFile);
            Materials.Add(material);
        }
        
        if (cpuFile.Position != Header.Shared.TextureNamesOffset)
        {
            //We should be at the texture names offset now
            throw new Exception("Unexpected static mesh file structure. Texture names offset not expected location.");
        }

        cpuFile.Seek(Header.Shared.TextureNamesOffset, SeekOrigin.Begin);
        foreach (var material in Materials)
        {
            foreach (var texture in material.Textures)
            {
                cpuFile.Seek(Header.Shared.TextureNamesOffset + texture.NameOffset, SeekOrigin.Begin);
                TextureNames.Add(cpuFile.ReadAsciiNullTerminatedString());
            }
        }

        cpuFile.Seek(Header.LodSubmeshIdOffset, SeekOrigin.Begin);
        for (var i = 0; i < Header.NumLods; i++)
        {
            LodSubmeshIds.Add(cpuFile.ReadInt32());
        }
        
        if (cpuFile.Position != Header.TagsOffset)
        {
            throw new Exception("Unexpected static mesh file structure. Mesh tags not expected location.");
        }
        
        Header.NumTags = cpuFile.ReadUInt32();
        for (var i = 0; i < Header.NumTags; i++)
        {
            MeshTag tag = new();
            tag.Read(cpuFile);
            Tags.Add(tag);
        }
        
        LoadedCpuFile = true;
    }

    public MeshInstanceData ReadData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call StaticMesh.ReadHeader() before calling StaticMesh.ReadData() on StaticMesh");
        }
        
        //Read index buffer
        gpuFile.Seek(Config.IndicesOffset, SeekOrigin.Begin);
        var indicesSizeInBytes = Config.NumIndices * Config.IndexSize;
        var indices = gpuFile.ReadBytes((int)indicesSizeInBytes);
        
        //Read vertex buffer
        gpuFile.Seek(Config.VerticesOffset, SeekOrigin.Begin);
        var verticesSize = Config.NumVertices * Config.VertexStride0;
        var vertices = gpuFile.ReadBytes((int)verticesSize);

        return new MeshInstanceData(Config, vertices, indices);
    }
}