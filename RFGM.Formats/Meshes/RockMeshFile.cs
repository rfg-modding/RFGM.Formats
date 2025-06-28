using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

//.cstch_pc/.gstch_pc files
public class RockMeshFile
{
    public string Name;
    public bool LoadedCpuFile { get; private set; }

    public MeshConfig Config = new();
    public List<string> TextureNames = new();

    public RockMeshFile(string name)
    {
        Name = name;
    }

    public void ReadHeader(Stream cpuFile)
    {
        //TODO: See if signature + version can check can be added here. Not all formats have them so may not be possible

        //Read the offset of the MeshDataBlock. Always at offset 16
        cpuFile.Seek(16, SeekOrigin.Begin);
        var meshConfigOffset = cpuFile.ReadUInt32();
        
        //Read the mesh config
        cpuFile.Seek(meshConfigOffset, SeekOrigin.Begin);
        Config.Read(cpuFile);
        
        //Read texture names
        cpuFile.AlignRead(16);
        var textureNamesSize = cpuFile.ReadUInt32();
        TextureNames = cpuFile.ReadSizedStringList(textureNamesSize);
        
        LoadedCpuFile = true;
    }
    
    public MeshInstanceData ReadData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call RockMeshFile.ReadHeader() before calling RockMeshFile.ReadData() on RockMeshFile");
        }
        
        //Read index buffer
        gpuFile.Seek(16, SeekOrigin.Begin);
        var indices = new byte[Config.NumIndices * Config.IndexSize];
        gpuFile.ReadExactly(indices);
        
        //Read vertex buffer
        gpuFile.AlignRead(16);
        var vertices = new byte[Config.NumVertices * Config.VertexStride0];
        gpuFile.ReadExactly(vertices);

        return new MeshInstanceData(Config, vertices, indices);
    }
}