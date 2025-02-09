using System.Numerics;
using RFGM.Formats.Materials;
using RFGM.Formats.Math;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class VfxMesh
{
    public uint NameOffset;
    public VfxMeshType MeshType;
    public int NumPositionKeys;
    public uint PositionKeysOffset;
    public int NumRotationKeys;
    public uint RotationKeysOffset;
    public int NumScaleKeys;
    public uint ScaleKeysOffset;
    public int NumOpacityKeys;
    public uint OpacityKeysOffset;
    public uint RenderMeshOffset;
    public uint MaterialMapDataOffset;
    public uint MaterialMapOffset;
    public uint NumMaterials;
    public uint RenderMaterialsOffset;
    public uint CollisionModelDataOffset;
    public uint ShapeMaterialIndex;
    public float ShapeVolume;
    public Vector3 ShapeCenterOfMass;
    public Matrix3x3 ShapeInertiaTensor;
    
    //Additional data at the various offsets
    public string Name;
    public List<VfxVectorKey> PositionKeys = new();
    public List<VfxQuaternionKey> RotationKeys = new();
    public List<VfxVectorKey> ScaleKeys = new();
    public List<VfxFloatKey> OpacityKeys = new();
    public List<RfgMaterial> Materials = new();
    public MeshConfig? RenderMeshConfig;
    
    private const long SizeInFile = 176;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        NameOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        MeshType = (VfxMeshType)stream.ReadUInt32();
        NumPositionKeys = stream.ReadInt32();
        
        PositionKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRotationKeys = stream.ReadInt32();
        stream.Skip(4); //Padding
        
        RotationKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumScaleKeys = stream.ReadInt32();
        stream.Skip(4); //Padding
        
        ScaleKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumOpacityKeys = stream.ReadInt32();
        stream.Skip(4); //Padding
        
        OpacityKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        RenderMeshOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        MaterialMapDataOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        MaterialMapOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumMaterials = stream.ReadUInt32();
        stream.Skip(4); //Padding
        
        RenderMaterialsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        CollisionModelDataOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        ShapeMaterialIndex = stream.ReadUInt32();
        ShapeVolume = stream.ReadFloat();
        ShapeCenterOfMass = stream.ReadStruct<Vector3>();
        ShapeInertiaTensor = stream.ReadStruct<Matrix3x3>();
        
        var endPos = stream.Position;
#if DEBUG
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxMesh. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
        
        //Read additional data from the above offsets
        if (NameOffset != uint.MaxValue)
        {
            stream.Seek(NameOffset, SeekOrigin.Begin);
            Name = stream.ReadAsciiNullTerminatedString();
        }

        if (RenderMeshOffset != uint.MaxValue)
        {
            RenderMeshConfig = new MeshConfig();
            stream.Seek(RenderMeshOffset, SeekOrigin.Begin);
            RenderMeshConfig.Read(stream);
        }

        if (NumPositionKeys > 0 && PositionKeysOffset != uint.MaxValue)
        {
            stream.Seek(PositionKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumPositionKeys; i++)
            {
                VfxVectorKey positionKey = new();
                positionKey.Read(stream);
                PositionKeys.Add(positionKey);
            }
        }

        if (NumRotationKeys > 0 && RotationKeysOffset != uint.MaxValue)
        {
            stream.Seek(RotationKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumRotationKeys; i++)
            {
                VfxQuaternionKey rotationKey = new();
                rotationKey.Read(stream);
                RotationKeys.Add(rotationKey);
            }
        }

        if (NumScaleKeys > 0 && ScaleKeysOffset != uint.MaxValue)
        {
            stream.Seek(ScaleKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumScaleKeys; i++)
            {
                VfxVectorKey scaleKey = new();
                scaleKey.Read(stream);
                ScaleKeys.Add(scaleKey);
            }
        }

        if (NumOpacityKeys > 0 && OpacityKeysOffset != uint.MaxValue)
        {
            stream.Seek(OpacityKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumOpacityKeys; i++)
            {
                VfxFloatKey opacityKey = new();
                opacityKey.Read(stream);
                OpacityKeys.Add(opacityKey);
            }
        }

        //Note: On all vanilla cefct_pc files MaterialMapOffset is 0
        if (MaterialMapDataOffset != uint.MaxValue)
        {
            stream.Seek(MaterialMapDataOffset, SeekOrigin.Begin);
            int offset = stream.ReadInt32();
            uint numMaterials = stream.ReadUInt32(); //Note: For all vanilla cefct_pc files this is 1
            stream.Skip(offset - 8);

            for (int i = 0; i < numMaterials; i++)
            {
                RfgMaterial material = new();
                material.Read(stream);
                Materials.Add(material);
            }
        }

        if (CollisionModelDataOffset != uint.MaxValue)
        {
            //TODO: This is some havok data. Can read it here once we figure it out. Its used in many different RFG mesh formats.
            //NOTE: When writing to cefct_pc files you should probably preserve this data.
            stream.Seek(CollisionModelDataOffset, SeekOrigin.Begin);
        }
        
        stream.Seek(endPos, SeekOrigin.Begin);
    }
}