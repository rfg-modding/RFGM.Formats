using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class VfxFilter
{
    public uint NameOffset;
    public VfxFilterType FilterType;
    public VfxFilterFlags Flags;
    public uint ClassId;
    public int ParentIndex;
    
    public int NumAffectedEmitters;
    public uint AffectedEmittersOffset;
    
    public int NumAffectedMeshes;
    public uint AffectedMeshesOffset;
    
    public int NumPositionKeys;
    public uint PositionKeysOffset;
    
    public int NumRotationKeys;
    public uint RotationKeysOffset;

    public uint ParticleSystemFilterOffset; //Note: Appears to only be valid at runtime

    public int UniqueDataSize;
    public uint UniqueDataOffset;

    public string Name = string.Empty;
    public List<int> AffectedEmitters = new();
    public List<int> AffectedMeshes = new();
    public List<VfxVectorKey> PositionKeys = new();
    public List<VfxQuaternionKey> RotationKeys = new();
    public byte[] UniqueData = [];
    
    private const long SizeInFile = 112;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        NameOffset = stream.ReadUInt32();
        stream.Skip(4);
        FilterType = (VfxFilterType)stream.ReadInt32();
        Flags = (VfxFilterFlags)stream.ReadInt32();
        ClassId = stream.ReadUInt32();
        ParentIndex = stream.ReadInt32();
        
        NumAffectedEmitters = stream.ReadInt32();
        stream.Skip(4);
        AffectedEmittersOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumAffectedMeshes = stream.ReadInt32();
        stream.Skip(4);
        AffectedMeshesOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumPositionKeys = stream.ReadInt32();
        stream.Skip(4);
        PositionKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRotationKeys = stream.ReadInt32();
        stream.Skip(4);
        RotationKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        ParticleSystemFilterOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        UniqueDataSize = stream.ReadInt32();
        stream.Skip(4);
        UniqueDataOffset = stream.ReadUInt32();
        stream.Skip(4);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxFilter. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif

        if (NameOffset != uint.MaxValue)
        {
            stream.Seek(NameOffset, SeekOrigin.Begin);
            Name = stream.ReadAsciiNullTerminatedString();
        }

        if (NumAffectedEmitters > 0)
        {
            stream.Seek(AffectedEmittersOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumAffectedEmitters; i++)
            {
                AffectedEmitters.Add(stream.ReadInt32());
            }
        }

        if (NumAffectedMeshes > 0)
        {
            stream.Seek(AffectedMeshesOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumAffectedMeshes; i++)
            {
                AffectedMeshes.Add(stream.ReadInt32());
            }
        }

        if (NumPositionKeys > 0)
        {
            stream.Seek(PositionKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumPositionKeys; i++)
            {
                VfxVectorKey key = new();
                key.Read(stream);
                PositionKeys.Add(key);
            }
        }

        if (NumRotationKeys > 0)
        {
            stream.Seek(RotationKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumRotationKeys; i++)
            {
                VfxQuaternionKey key = new();
                key.Read(stream);
                RotationKeys.Add(key);
            }
        }
        
        if (UniqueDataOffset > 0 && UniqueDataOffset != uint.MaxValue && UniqueDataSize > 0)
        {
            //TODO: Figure out what this data is
            stream.Seek(UniqueDataOffset, SeekOrigin.Begin);
            UniqueData = new byte[UniqueDataSize];
            stream.ReadExactly(UniqueData);
        }
        
        stream.Seek(endPos, SeekOrigin.Begin);
    }
}