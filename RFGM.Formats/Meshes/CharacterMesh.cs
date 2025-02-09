using RFGM.Formats.Materials;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

//RFGR character mesh format. Extension: ccmesh_pc|gcmesh_pc
public class CharacterMesh(string name)
{
    public string Name = name;
    
    public CharacterMeshHeader Header;
    public MeshConfig Config = new();
    public List<uint> MaterialOffsets = new();
    public List<RfgMaterial> Materials = new();
    public List<string> TextureNames = new();
    public List<int> LodSubmeshIds = new();

    public List<uint> BoneIndices = new();
    public List<Sphere> Spheres = new();
    public List<Cylinder> Cylinders = new();

    public bool LoadedCpuFile { get; private set; }

    public void ReadHeader(Stream cpuFile)
    {
        Header.Read(cpuFile);
        
        if (Header.Shared.Signature != 0xFAC351A9)
            throw new Exception("Invalid character mesh file signature detected. Expected 0xFAC351A9.");
        if (Header.Shared.Version != 4)
            throw new Exception("Invalid character mesh file version detected. Expected 4.");

        cpuFile.Seek(Header.Shared.MeshOffset, SeekOrigin.Begin);
        Config.Read(cpuFile, patchBufferOffsets: true);
        
        //TODO: Align(16) may be needed here when an exporter is written.
        cpuFile.Seek(Header.Shared.MaterialMapOffset, SeekOrigin.Begin);

        //TODO: Determine if any other important data is between this and the material offsets. The null bytes might just be padding.
        var materialsOffsetRelative = cpuFile.ReadUInt32();
        var numMaterials = cpuFile.ReadUInt32();
        cpuFile.Seek(Header.Shared.MaterialsOffset, SeekOrigin.Begin);
        
        //Read material offsets
        for (var i = 0; i < numMaterials; i++)
        {
            MaterialOffsets.Add(cpuFile.ReadUInt32());
            cpuFile.Skip(4);
        }
        
        //Read materials
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
            throw new Exception("Unexpected character mesh structure. Texture names offset not expected location.");
        }
        
        cpuFile.Seek(Header.Shared.TextureNamesOffset, SeekOrigin.Begin);
        foreach (var material in Materials)
        {
            foreach (var textureDesc in material.Textures)
            {
                cpuFile.Seek(Header.Shared.TextureNamesOffset + textureDesc.NameOffset, SeekOrigin.Begin);
                TextureNames.Add(cpuFile.ReadAsciiNullTerminatedString());
            }
        }
        
        cpuFile.Seek(Header.LodSubmeshIdOffset, SeekOrigin.Begin);
        for (var i = 0; i < Header.NumLods; i++)
        {
            LodSubmeshIds.Add(cpuFile.ReadInt32());
        }

        cpuFile.Seek(Header.BoneIndicesOffset, SeekOrigin.Begin);
        for (var i = 0; i < Header.NumBones; i++)
        {
            BoneIndices.Add(cpuFile.ReadUInt32());
        }

        cpuFile.Seek(Header.SpheresOffset, SeekOrigin.Begin);
        for (var i = 0; i < Header.NumSpheres; i++)
        {
            Sphere sphere = new();
            sphere.Read(cpuFile);
            Spheres.Add(sphere);
        }

        cpuFile.Seek(Header.CylindersOffset, SeekOrigin.Begin);
        for (var i = 0; i < Header.NumCylinders; i++)
        {
            Cylinder cylinder = new();
            cylinder.Read(cpuFile);
            Cylinders.Add(cylinder);
        }
        
        LoadedCpuFile = true;
    }

    public MeshInstanceData ReadData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call CharacterMesh.ReadHeader() before calling CharacterMesh.ReadData() on CharacterMesh");
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