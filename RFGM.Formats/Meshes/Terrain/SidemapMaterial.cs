using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Terrain;

public class SidemapMaterial
{
    public List<string> MaterialNames = new();
    public RfgMaterial Material = new();

    public void Read(Stream stream)
    {
        var materialNamesSize = stream.ReadUInt32();
        MaterialNames = stream.ReadSizedStringList(materialNamesSize);
        stream.AlignRead(16);
        Material.Read(stream);
    }
}