using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public class StaticMesh
{
    public StaticMeshHeader Header = new();
    public MeshConfig Config = new();
    public List<uint> MaterialOffsets = new();
    public List<RfgMaterial> Materials = new();
    public List<string> TextureNames = new();
    public List<int> LodSubmeshIds = new();
    public List<MeshTag> Tags = new();

    public bool Loaded { get; private set; } = false;
    
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

    public void Read(Stream cpuData)
    {
        Header.Shared.Read(cpuData);
        Header.NumLods = cpuData.Read<uint>();
        cpuData.Skip(4);
        Header.LodSubmeshIdOffset = cpuData.Read<int>();
        cpuData.Skip(4);
        Header.TagsOffset = cpuData.Read<int>();
        cpuData.Skip(4);
        Header.NumTags = cpuData.Read<uint>();
        cpuData.Skip(4);
        Header.MeshTagOffset = cpuData.Read<uint>();
        cpuData.Skip(4);
        Header.CmIndex = cpuData.Read<uint>();
        cpuData.Skip(4);
        
        if (Header.Shared.Signature != 0xC0FFEE11)
            throw new Exception("Invalid static mesh file signature detected. Expected 0xC0FFEE11.");
        if (Header.Shared.Version != 5)
            throw new Exception("Invalid static mesh file version detected. Expected 5.");

        if (Header.NumLods > 1)
        {
            //TODO: Make sure there's multiple IDs at the LodSubmeshIdOffset in this case. Made this case fail to make sure we catch it.
            throw new Exception("Static meshes with more than one LOD level not supported. Please report this error with the mesh in question to the developer.");
        }
        
        //TODO: Double check that these seeks are from the beginning of the stream
        //TODO: Align(16) may be needed here
        cpuData.Seek(Header.Shared.MeshOffset, SeekOrigin.Begin);
        Config.Read(cpuData);
        
        //TODO: Align(16) may be needed here when an exporter is written.
        cpuData.Seek(Header.Shared.MaterialMapOffset, SeekOrigin.Begin);
        
        //TODO: Determine if any other important data is between this and the material offsets. The null bytes might just be padding.
        uint materialsOffsetRelative = cpuData.Read<uint>();
        uint numMaterials = cpuData.Read<uint>();
        cpuData.Seek(Header.Shared.MaterialsOffset, SeekOrigin.Begin);

        for (int i = 0; i < numMaterials; i++)
        {
            MaterialOffsets.Add(cpuData.Read<uint>());
            cpuData.Skip(4);
        }

        for (int i = 0; i < numMaterials; i++)
        {
            //TODO: Make sure we're not skipping any important data by doing this
            cpuData.Seek(MaterialOffsets[i], SeekOrigin.Begin);
            
            RfgMaterial material = new();
            material.Read(cpuData);
            Materials.Add(material);
        }
        
        if (cpuData.Position != Header.Shared.TextureNamesOffset)
        {
            //We should be at the texture names offset now
            throw new Exception("Unexpected static mesh file structure. Texture names offset not expected location.");
        }

        cpuData.Seek(Header.Shared.TextureNamesOffset, SeekOrigin.Begin);
        foreach (RfgMaterial material in Materials)
        {
            foreach (TextureDesc texture in material.Textures)
            {
                cpuData.Seek(Header.Shared.TextureNamesOffset + texture.NameOffset, SeekOrigin.Begin);
                TextureNames.Add(cpuData.ReadNullTerminatedString());
            }
        }

        cpuData.Seek(Header.LodSubmeshIdOffset, SeekOrigin.Begin);
        for (int i = 0; i < Header.NumLods; i++)
        {
            LodSubmeshIds.Add(cpuData.Read<int>());
        }
        
        if (cpuData.Position != Header.TagsOffset)
        {
            throw new Exception("Unexpected static mesh file structure. Mesh tags not expected location.");
        }
        
        Header.NumTags = cpuData.Read<uint>();
        for (int i = 0; i < Header.NumTags; i++)
        {
            MeshTag tag = new();
            tag.Read(cpuData);
            Tags.Add(tag);
        }
    }

    public MeshInstanceData? GetMeshInstanceData(Stream gpuData)
    {
        //TODO: IMPLEMENT
        return null;
    }
}