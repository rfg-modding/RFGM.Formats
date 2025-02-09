using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class ParticleMaterialData
{
    public int EffectRef;
    public short ShaderIndex;
    public short NumTextures;
    public int TextureOffset;
    public ushort NumShaderConstants;
    public int ShaderConstants;
    public ParticleConstantInfo VertexShaderConstantInfo = new();
    public ParticleConstantInfo PixelShaderConstantInfo = new();
    public ParticleMaterialDataFlags Flags;
        
    private const long SizeInFile = 48;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        EffectRef = stream.ReadInt32();
        ShaderIndex = stream.ReadInt16();
        NumTextures = stream.ReadInt16();
        TextureOffset = stream.ReadInt32();
        NumShaderConstants = stream.ReadUInt16();
        stream.Skip(2); //Padding
        ShaderConstants = stream.ReadInt32();
        VertexShaderConstantInfo.Read(stream);
        PixelShaderConstantInfo.Read(stream);
        Flags = (ParticleMaterialDataFlags)stream.ReadInt32();
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ParticleMaterialData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}