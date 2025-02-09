using System.Runtime.CompilerServices;
using RFGM.Formats.Materials;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class EffectFile(string filename)
{
    public string Filename { get; private set; } = filename;
    public bool LoadedCpuFile { get; private set; } = false;

    public EffectFileHeader Header = new();
    public string Name = string.Empty;
    public List<VfxExpression> Expressions = new();
    public List<VfxMesh> VfxMeshes = new();
    public List<(string textureName, long offset)> Textures = new();
    public List<(string textureName, long offset)> MeshTextures = new();
    public List<VfxEmitter> Emitters = new();
    public List<VfxLight> Lights = new();
    public List<VfxFilter> Filters = new();
    public List<VfxCorona> Coronas = new();
    
    public void ReadHeader(Stream cpuFile)
    {
        Header.Read(cpuFile);
        if (Header.Signature != 1463955767)
        {
            throw new Exception($"Unsupported effect file signature. Expected 1463955767, found {Header.Signature}");
        }
        if (Header.Version != 26)
        {
            throw new Exception($"Unsupported effect file version. Expected 26, found {Header.Version}");
        }
        Name = cpuFile.ReadAsciiNullTerminatedString();

        if (Header.NumExpressions > 0)
        {
            cpuFile.Seek(Header.ExpressionsOffset, SeekOrigin.Begin);
            for (int i = 0; i < Header.NumExpressions; i++)
            {
                VfxExpression vfxExpression = new();
                vfxExpression.Read(cpuFile);
                Expressions.Add(vfxExpression);
            }
        }
        
        if (Header.NumMeshes > 0)
        {
            cpuFile.Seek(Header.MeshesOffset, SeekOrigin.Begin);
            for (int i = 0; i < Header.NumMeshes; i++)
            {
                VfxMesh vfxMesh = new();
                vfxMesh.Read(cpuFile);
                VfxMeshes.Add(vfxMesh);
            }
        }

        if (Header.BitmapNamesOffset != uint.MaxValue && Header.NumBitmaps > 0)
        {
            cpuFile.Seek(Header.BitmapNamesOffset, SeekOrigin.Begin);
            long startOffset = cpuFile.Position;
            long textureOffset = 0;
            for (int i = 0; i < Header.NumBitmaps; i++)
            {
                string textureName = cpuFile.ReadAsciiNullTerminatedString();
                Textures.Add((textureName, textureOffset));
                textureOffset = cpuFile.Position - startOffset;
            }
        }

        if (Header.MeshBitmapsOffset != uint.MaxValue)
        {
            List<uint> distinctTextureNameOffsets = VfxMeshes.SelectMany(mesh => mesh.Materials).SelectMany(material => material.Textures).Select(texture => texture.NameOffset)
                .Distinct().ToList();
            cpuFile.Seek(Header.MeshBitmapsOffset, SeekOrigin.Begin);
            foreach (uint nameOffset in distinctTextureNameOffsets)
            {
                cpuFile.Seek(Header.MeshBitmapsOffset + nameOffset, SeekOrigin.Begin);
                string textureName = cpuFile.ReadAsciiNullTerminatedString();
                MeshTextures.Add((textureName, nameOffset));
            }

            foreach ((string textureName, long offset) in MeshTextures)
            {
                foreach (VfxMesh mesh in VfxMeshes)
                {
                    foreach (RfgMaterial material in mesh.Materials)
                    {
                        for (var i = 0; i < material.Textures.Count; i++)
                        {
                            if (material.Textures[i].NameOffset == offset)
                            {
                                material.Textures[i] = material.Textures[i] with { Name = textureName };
                            }
                        }
                    }
                }
            }
        }

        if (Header.NumEmitters > 0)
        {
            cpuFile.Seek(Header.EmittersOffset, SeekOrigin.Begin);
            for (int i = 0; i < Header.NumEmitters; i++)
            {
                VfxEmitter vfxEmitter = new();
                vfxEmitter.Read(cpuFile);
                Emitters.Add(vfxEmitter);
            }
        }

        if (Header.NumLights > 0)
        {
            cpuFile.Seek(Header.LightsOffset, SeekOrigin.Begin);
            for (int i = 0; i < Header.NumLights; i++)
            {
                VfxLight vfxLight = new();
                vfxLight.Read(cpuFile);
                Lights.Add(vfxLight);
            }
        }

        if (Header.NumFilters > 0)
        {
            cpuFile.Seek(Header.FiltersOffset, SeekOrigin.Begin);
            for (int i = 0; i < Header.NumFilters; i++)
            {
                VfxFilter filter = new();
                filter.Read(cpuFile);
                Filters.Add(filter);
            }
        }

        if (Header.NumCoronas > 0)
        {
            cpuFile.Seek(Header.CoronasOffset, SeekOrigin.Begin);
            for (int i = 0; i < Header.NumCoronas; i++)
            {
                VfxCorona corona = new();
                corona.Read(cpuFile);
                Coronas.Add(corona);
            }
        }
        
        //TODO: Figure out what the data that lies between some of these offsets is. E.g. The data at offset 320 in mp_rhino_shield.cefct_pc. Has known data before and after some of the header offsets.
        //TODO: Investigate whether there are other areas of the cefct_pc files that are getting skipped with the current approach of seeking to offsets.
        //TODO: ^^ Easiest way is probably to make a Stream wrapper that tracks which regions have been read and run it on every single cefct_pc file.
        LoadedCpuFile = true;
    }

    public MeshInstanceData? ReadMeshData(Stream gpuFile, int index)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call ReadHeader() before calling ReadMeshData()");
        }
        if (index < 0 || index >= VfxMeshes.Count)
        {
            throw new Exception($"Mesh index {index} out of range in EffectFile.ReadMeshData()");
        }

        //Calculate mesh data offset in gpu file
        long meshStartPos = 0;
        for (var i = 0; i < index; i++)
        {
            var mesh = VfxMeshes[i].RenderMeshConfig;
            if (mesh == null)
                throw new Exception($"No render mesh data found for mesh at index {index} EffectFile.ReadMeshData()");

            meshStartPos += 4; //Start crc
            meshStartPos += StreamHelpers.CalcAlignment(meshStartPos, 16);
            meshStartPos += mesh.NumIndices * mesh.IndexSize;
            meshStartPos += StreamHelpers.CalcAlignment(meshStartPos, 16);
            meshStartPos += mesh.NumVertices * mesh.VertexStride0;
            meshStartPos += 4; //Skip end CRC
            meshStartPos += StreamHelpers.CalcAlignment(meshStartPos, 16);
        }

        if (index == 0)
        {
            
        }

        var config = VfxMeshes[index].RenderMeshConfig;
        if (config == null)
        {
            throw new Exception($"No render mesh data found for mesh at index {index} EffectFile.ReadMeshData()");
        }

        //Sanity check. Make sure CRCs match. If not something probably went wrong when reading/writing from packfile
        gpuFile.Seek(meshStartPos, SeekOrigin.Begin);
        var startCRC = gpuFile.ReadUInt32();
        if (startCRC != config.VerificationHash)
        {
            throw new Exception($"Start CRC mismatch in gefct_pc file {Name}");
        }
        
        //Read index buffer
        gpuFile.AlignRead(16);
        var indexBufferSize = config.NumIndices * config.IndexSize;
        var indexBuffer = new byte[indexBufferSize];
        gpuFile.ReadExactly(indexBuffer);

        //Read vertex buffer
        gpuFile.AlignRead(16);
        var vertexBufferSize = config.NumVertices * config.VertexStride0;
        var vertexBuffer = new byte[vertexBufferSize];
        gpuFile.ReadExactly(vertexBuffer);
        
        //Sanity check. CRC at the end of the mesh data should match the start
        var endCRC = gpuFile.ReadUInt32();
        if (startCRC != endCRC)
        {
            throw new Exception($"End CRC mismatch in gefct_pc file {Name}");
        }

        return new MeshInstanceData(config, vertexBuffer, indexBuffer);
    }
}