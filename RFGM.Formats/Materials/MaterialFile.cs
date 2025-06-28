using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Materials;

//For .mat_pc files. These are standalone versions of the render material data that can be found in any of the RFG mesh formats.
public class MaterialFile
{
    public string Filename = string.Empty;

    public uint Signature;
    public uint Version;

    public int TextureNamesOffset;
    public List<RfgMaterial> Materials = new();
    public List<string> TextureNames = new();
    
    public bool LoadedCpuFile { get; private set; }

    public void Read(Stream stream)
    {
        Signature = stream.ReadUInt32();
        if (Signature != 2954754766)
            throw new Exception($"Unexpected .mat_pc file signature. Expected 2954754766, found {Signature}");
        
        Version = stream.ReadUInt32();
        if (Version != 2)
            throw new Exception($"Unexpected .mat_pc file version. Expected 2, found {Version}");
        
        //TODO: Figure out what these unknown fields are
        uint unk0 = stream.ReadUInt32();
        stream.Skip(4);
        
        uint unk1 = stream.ReadUInt32();
        stream.Skip(4);
        
        uint unk2 = stream.ReadUInt32();
        stream.Skip(4);
        
        TextureNamesOffset = stream.ReadInt32();
        stream.AlignRead(16);
        
        uint materialMapOffsetData = stream.ReadUInt32(); //Always equals 16 in the vanilla files
        uint numMaterials = stream.ReadUInt32();
        
        List<uint> unknownMaterialData = new();
        for (int i = 0; i < numMaterials; i++)
        {
            unknownMaterialData.Add(stream.ReadUInt32()); //TODO: Figure out what this data is. It looks like it might be junk data that only gets set at runtime
        }
        stream.AlignRead(16);
        stream.Skip(numMaterials * 8); //TODO: Figure out what this data is

        if (numMaterials > 0)
        {
            stream.AlignRead(16);
            for (int i = 0; i < numMaterials; i++)
            {
                stream.AlignRead(16);
                RfgMaterial material = new();
                material.Read(stream);
                Materials.Add(material);
            }
        }
        
        if (stream.Position != TextureNamesOffset)
        {
            //We should be at the texture names offset now
            throw new Exception("Unexpected .mat_pc file structure. Texture names offset not expected location.");
        }
        
        stream.Seek(TextureNamesOffset, SeekOrigin.Begin);
        foreach (var material in Materials)
        {
            for (var i = 0; i < material.Textures.Count; i++)
            {
                stream.Seek(TextureNamesOffset + material.Textures[i].NameOffset, SeekOrigin.Begin);
                string textureName = stream.ReadAsciiNullTerminatedString();
                TextureNames.Add(textureName);
                
                TextureDesc texture = material.Textures[i];
                material.Textures[i] = texture with { Name = textureName };
            }
        }

        LoadedCpuFile = true;
    }
}