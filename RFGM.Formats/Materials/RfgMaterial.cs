using RFGM.Formats.Hashes;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Materials;

public class RfgMaterial
{
    public uint ShaderHandle;
    public uint NameChecksum;
    public RfgMaterialFlags MaterialFlags;
    public ushort NumTextures;
    public byte NumConstants;
    public byte MaxConstants;
    public uint TextureOffset;
    public uint ConstantNameChecksumsOffset;
    public uint ConstantBlockOffset;

    public List<TextureDesc> Textures = new();
    public List<uint> ConstantNameChecksums = new();
    public List<MaterialConstant> Constants = new();

    public string Name => HashDictionary.FindOriginString(NameChecksum) ?? "Unknown";
    public string ShaderName => HashDictionary.FindOriginString(ShaderHandle) ?? "Unknown";
    public IEnumerable<string> ConstantNames => ConstantNameChecksums.Select(hash => HashDictionary.FindOriginString(hash) ?? "Unknown");

    public void Read(Stream stream)
    {
        var materialDataStart = stream.Position;
        var materialDataSize = stream.ReadUInt32();
        
        ShaderHandle = stream.ReadUInt32();
        NameChecksum = stream.ReadUInt32();
        MaterialFlags = (RfgMaterialFlags)stream.ReadUInt32();
        NumTextures = stream.ReadUInt16();
        NumConstants = stream.ReadUInt8();
        MaxConstants = stream.ReadUInt8();
        TextureOffset = stream.ReadUInt32();
        ConstantNameChecksumsOffset = stream.ReadUInt32();
        ConstantBlockOffset = stream.ReadUInt32();

        for (var i = 0; i < NumTextures; i++)
        {
            TextureDesc textureDesc = new();
            textureDesc.Read(stream);
            Textures.Add(textureDesc);
        }

        for (var i = 0; i < NumConstants; i++)
        {
            ConstantNameChecksums.Add(stream.ReadUInt32());
        }

        stream.AlignRead(16);
        for (var i = 0; i < MaxConstants; i++)
        {
            MaterialConstant constant = new();
            constant.Read(stream);
            Constants.Add(constant);
        }
        stream.AlignRead(16);
        
        if (stream.Position != materialDataStart + materialDataSize)
            throw new Exception("RfgMaterial was not the expected size");
    }
}