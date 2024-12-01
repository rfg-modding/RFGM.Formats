using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public class RfgMaterial
{
    public uint ShaderHandle;
    public uint NameChecksum;
    public uint MaterialFlags;
    public ushort NumTextures;
    public byte NumConstants;
    public byte MaxConstants;
    public uint TextureOffset;
    public uint ConstantNameChecksumsOffset;
    public uint ConstantBlockOffset;

    public List<TextureDesc> Textures = new();
    public List<uint> ConstantNameChecksums = new();
    public List<MaterialConstant> Constants = new();

    public void Read(Stream stream)
    {
        long materialDataStart = stream.Position;
        uint materialDataSize = stream.Read<uint>();
        
        ShaderHandle = stream.Read<uint>();
        NameChecksum = stream.Read<uint>();
        MaterialFlags = stream.Read<uint>();
        NumTextures = stream.Read<ushort>();
        NumConstants = stream.Read<byte>();
        MaxConstants = stream.Read<byte>();
        TextureOffset = stream.Read<uint>();
        ConstantNameChecksumsOffset = stream.Read<uint>();
        ConstantBlockOffset = stream.Read<uint>();

        for (int i = 0; i < NumTextures; i++)
        {
            TextureDesc textureDesc = new();
            textureDesc.Read(stream);
            Textures.Add(textureDesc);
        }

        for (int i = 0; i < NumConstants; i++)
        {
            ConstantNameChecksums.Add(stream.Read<uint>());
        }

        stream.AlignRead(16);
        for (int i = 0; i < MaxConstants; i++)
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