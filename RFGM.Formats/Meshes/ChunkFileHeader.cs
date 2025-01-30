using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public struct ChunkFileHeader()
{
    //TODO: Most of these XXXOffset values are likely only set to valid values at runtime. Remove them from this class if we're sure of that.
    public uint Signature = 0;
    public uint Version = 0;
    public uint SourceVersion = 0;
    public uint RenderDataChecksum = 0;
    public uint RenderCpuDataOffset = 0;
    public uint RenderCpuDataSize = 0;
    public uint CollisionModelChecksum = 0;
    public uint CollisionModelDataOffset = 0;
    public uint CollisionModelDataSize = 0;
    public uint DestructionChecksum = 0;
    public uint DestructionOffset = 0;
    public uint DestructionDataSize = 0;
    public Vector3 Bmin;
    public Vector3 Bmax;
    public uint RenderMeshOffset;
    public uint MaterialMapOffset;
    public uint MaterialsOffset;
    public uint NumRenderMaterials;
    public int NumGeneralObjects;
    public uint GeneralObjectOffset;
    public int NumBaseDestroyables;
    public uint BaseDestroyablesOffset;
    public uint RbbParentLodsOffset;
    public uint NextChunkOffset; //Likely only valid at runtime. Added here just in case.
    public uint PrevChunkOffset; //Likely only valid at runtime. Added here just in case.
    public ushort InternalUid;
    public ushort InstRefCount;
    public uint StreamDataOffset;
    public int DataSize;
    public int NumScriptPoints;
    public uint ScriptPointsOffset;
    public uint RbbShardsOffset;
    public int NumObjectIdentifiers;
    public uint ObjectIdentifiersOffset;
    public int NumObjectSkirts;
    public uint ObjectSkirtsOffset;
    public uint MoppHandle;
    public uint TextureNamesSize;
    public uint TextureNamesOffset;
    public int NumLightClipObjects;
    public uint LightClipObjectsOffset;
    public uint RenderGpuDataOffset;
    public uint RenderGpuDataSize;
    public uint BaseDestroyableKdtreesOffset;
    public uint PhysicsDataOffset;
    public bool PhysicsDataIsLoaded;
    
    private const long SizeInFile = 656;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        Signature = stream.ReadUInt32();
        Version = stream.ReadUInt32();
        SourceVersion = stream.ReadUInt32();
        RenderDataChecksum = stream.ReadUInt32();
        RenderCpuDataOffset = stream.ReadUInt32();
        RenderCpuDataSize = stream.ReadUInt32();
        CollisionModelChecksum = stream.ReadUInt32();
        CollisionModelDataOffset = stream.ReadUInt32();
        CollisionModelDataSize = stream.ReadUInt32();
        DestructionChecksum = stream.ReadUInt32();
        DestructionOffset = stream.ReadUInt32();
        DestructionDataSize = stream.ReadUInt32();
        stream.Skip(128); //The game labels this "name" but its always empty in the file. Set by the game at runtime
        stream.Skip(128); //The game labels this "filename" but its always empty in the file. Set by the game at runtime
        Bmin = stream.ReadStruct<Vector3>();
        Bmax = stream.ReadStruct<Vector3>();
        
        RenderMeshOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        MaterialMapOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        MaterialsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRenderMaterials = stream.ReadUInt32();
        NumGeneralObjects = stream.ReadInt32();
        
        GeneralObjectOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumBaseDestroyables = stream.ReadInt32();
        stream.Skip(4); //Padding
        
        BaseDestroyablesOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        RbbParentLodsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NextChunkOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        PrevChunkOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        InternalUid = stream.ReadUInt16();
        InstRefCount = stream.ReadUInt16();
        stream.Skip(4); //Padding

        StreamDataOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        DataSize = stream.ReadInt32();
        NumScriptPoints = stream.ReadInt32();
        
        ScriptPointsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        RbbShardsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumObjectIdentifiers = stream.ReadInt32();
        stream.Skip(4); //Padding

        ObjectIdentifiersOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumObjectSkirts = stream.ReadInt32();
        stream.Skip(4); //Padding
        
        ObjectSkirtsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        MoppHandle = stream.ReadUInt32();
        TextureNamesSize = stream.ReadUInt32();
        
        TextureNamesOffset = stream.ReadUInt32();
        stream.Skip(4);

        NumLightClipObjects = stream.ReadInt32();
        stream.Skip(4); //Padding

        LightClipObjectsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        RenderGpuDataOffset = stream.ReadUInt32();
        RenderGpuDataSize = stream.ReadUInt32();
        
        BaseDestroyableKdtreesOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        PhysicsDataOffset = stream.ReadUInt32();
        stream.Skip(4);
        PhysicsDataIsLoaded = stream.ReadBoolean();
        
        stream.Skip(115); //Padding. The game has this labelled as "future use"
        stream.Skip(4); //Padding
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ChunkFileHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}