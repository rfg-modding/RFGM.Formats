namespace RFGM.Formats.Meshes.Shared;

public class MeshInstanceData(MeshConfig config, byte[] vertices, byte[] indices)
{
    public MeshConfig Config = config;
    public byte[] Vertices = vertices;
    public byte[] Indices = indices;
}