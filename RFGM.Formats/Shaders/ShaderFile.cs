using RFGM.Formats.Streams;

namespace RFGM.Formats.Shaders;

//For .fxo_kg files. These have some RFG specific data with DirectX11 shader bytecode appended onto them.
public class ShaderFile
{
    //Note: Size of initial header data is 52 bytes
    public uint Signature;
    public uint Version;
    public uint ShaderFlags;
    public short NumTexFixups;
    public short NumConstFixups;
    public short NumBoolFixups;
    public byte NumVertexShaders;
    public byte NumPixelShaders;
    public int VertexShadersOffset;
    public int PixelShadersOffset;
    public int TexFixupsOffset;
    public int ConstFixupsOffset;
    public int BoolFixupsOffset;
    public byte BaseTechniqueIndex;
    public byte LightingTechniqueIndex;
    public byte DepthOnlyTechniqueIndex;
    public byte UserTechniqueStartIndex;
    public int ShaderTechniquesOffset;
    public int NumTechniques;

    public string Filename = string.Empty;
    public bool LoadedCpuFile { get; private set; }

    public List<TexFixup> TexFixups = new();
    public List<ConstFixup> ConstFixups = new();
    public List<ConstFixup> BoolFixups = new();
    public List<uint> VertexShaderSizes = new();
    public List<uint> PixelShaderSizes = new();
    public List<ShaderTechnique> Techniques = new();
    public List<byte[]> VertexShadersBytecode = new();
    public List<byte[]> PixelShadersBytecode = new();
    
    public void Read(Stream stream)
    {
        Signature = stream.ReadUInt32();
        if (Signature != 1262658030)
            throw new Exception($"Unexpected .fxo_kg file signature. Expected 1262658030, found {Signature}");
        
        Version = stream.ReadUInt32();
        if (Version != 7)
            throw new Exception($"Unexpected .fxo_kg file version. Expected 7, found {Version}");
        
        ShaderFlags = stream.ReadUInt32();
        NumTexFixups = stream.ReadInt16();
        NumConstFixups = stream.ReadInt16();
        NumBoolFixups = stream.ReadInt16();
        NumVertexShaders = stream.ReadUInt8();
        NumPixelShaders = stream.ReadUInt8();
        VertexShadersOffset = stream.ReadInt32();
        PixelShadersOffset = stream.ReadInt32();
        TexFixupsOffset = stream.ReadInt32();
        ConstFixupsOffset = stream.ReadInt32();
        BoolFixupsOffset = stream.ReadInt32();
        BaseTechniqueIndex = stream.ReadUInt8();
        LightingTechniqueIndex = stream.ReadUInt8();
        DepthOnlyTechniqueIndex = stream.ReadUInt8();
        UserTechniqueStartIndex = stream.ReadUInt8();
        ShaderTechniquesOffset = stream.ReadInt32();
        NumTechniques = stream.ReadInt32();

        if (stream.Position != 52)
        {
            throw new Exception($"Incorrect fxo_kg file header end offset. Expected 52, ended up at {stream.Position}. Make sure the stream is the start of the file before calling ShaderFile.Read()");
        }

        //TODO: There's still many tex fixups, const fixups, and technique names that we don't know. Figure them out
        for (int i = 0; i < NumTexFixups; i++)
        {
            TexFixup fixup = new();
            fixup.Read(stream);
            TexFixups.Add(fixup);
        }
        
        for (int i = 0; i < NumConstFixups; i++)
        {
            ConstFixup fixup = new();
            fixup.Read(stream);
            ConstFixups.Add(fixup);
        }

        for (int i = 0; i < NumBoolFixups; i++)
        {
            ConstFixup fixup = new();
            fixup.Read(stream);
            BoolFixups.Add(fixup);
        }
        stream.AlignRead(4);

        for (int i = 0; i < NumVertexShaders; i++)
        {
            VertexShaderSizes.Add(stream.ReadUInt32());
            stream.Skip(4); //Always junk data. Likely only set at runtime
        }
        for (int i = 0; i < NumPixelShaders; i++)
        {
            PixelShaderSizes.Add(stream.ReadUInt32());
        }
        stream.AlignRead(4);

        for (int i = 0; i < NumTechniques; i++)
        {
            ShaderTechnique technique = new();
            technique.Read(stream);
            Techniques.Add(technique);
        }
        stream.AlignRead(4);
        
        //TODO: Make native dll/so with DXBC decompiler (from dxvk) + SPIRV-Cross to convert these shaders to GLSL
        //Read compiled vertex shader bytecode
        for (int i = 0; i < NumVertexShaders; i++)
        {
            stream.AlignRead(16);
            uint shaderStartSig = stream.Peek<uint>();
            if (shaderStartSig != 1128421444) //'DXBC' ASCII
            {
                throw new Exception($"Unexpected data in shader file at position {stream.Position}. Expected shader magic 'DXBC'. Found {shaderStartSig}");
            }
                
            byte[] shaderBytes = new byte[VertexShaderSizes[i]];
            stream.ReadExactly(shaderBytes);
            VertexShadersBytecode.Add(shaderBytes);
        }
        
        //Read compiled pixel shader bytecode
        for (int i = 0; i < NumPixelShaders; i++)
        {
            stream.AlignRead(16);
            uint shaderStartSig = stream.Peek<uint>();
            if (shaderStartSig != 1128421444) //'DXBC' ASCII
            {
                throw new Exception($"Unexpected data in shader file at position {stream.Position}. Expected shader magic 'DXBC'. Found {shaderStartSig}");
            }
                
            byte[] shaderBytes = new byte[PixelShaderSizes[i]];
            stream.ReadExactly(shaderBytes);
            PixelShadersBytecode.Add(shaderBytes);
        }

        LoadedCpuFile = true;
    }
}