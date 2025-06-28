using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class VfxLight
{
    public uint NameOffset;
    public short Flags;
    public short Type;
    
    public int NumColorKeys;
    public uint ColorKeysOffset;
    
    public int NumIntensityKeys;
    public uint IntensityKeysOffset;

    public int NumPositionKeys;
    public uint PositionKeysOffset;

    public int NumFvecKeys;
    public uint FvecKeysOffset;

    public float HotspotSize;
    public float HotspotFalloffSize;
    public float AttenuationStart;
    public float AttenuationEnd;
    public int ParentIndex;

    public string Name = string.Empty;
    public List<VfxVectorKey> ColorKeys = new();
    public List<VfxFloatKey> IntensityKeys = new();
    public List<VfxVectorKey> PositionKeys = new();
    public List<VfxVectorKey> FvecKeys = new();
    
    private const long SizeInFile = 96;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        NameOffset = stream.ReadUInt32();
        stream.Skip(4);
        Flags = stream.ReadInt16();
        Type = stream.ReadInt16();
        
        NumColorKeys = stream.ReadInt32();
        ColorKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumIntensityKeys = stream.ReadInt32();
        stream.Skip(4);
        IntensityKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumPositionKeys = stream.ReadInt32();
        stream.Skip(4);
        PositionKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumFvecKeys = stream.ReadInt32();
        stream.Skip(4);
        FvecKeysOffset = stream.ReadUInt32();
        stream.Skip(4);

        HotspotSize = stream.ReadFloat();
        HotspotFalloffSize = stream.ReadFloat();
        AttenuationStart = stream.ReadFloat();
        AttenuationEnd = stream.ReadFloat();
        ParentIndex = stream.ReadInt32();
        stream.Skip(4);
        
        var endPos = stream.Position;
#if DEBUG
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxLight. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif

        if (NameOffset != uint.MaxValue)
        {
            stream.Seek(NameOffset, SeekOrigin.Begin);
            Name = stream.ReadAsciiNullTerminatedString();
        }

        if (NumColorKeys > 0)
        {
            stream.Seek(ColorKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumColorKeys; i++)
            {
                VfxVectorKey key = new();
                key.Read(stream);
                ColorKeys.Add(key);
            }
        }

        if (NumIntensityKeys > 0)
        {
            stream.Seek(IntensityKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumIntensityKeys; i++)
            {
                VfxFloatKey key = new();
                key.Read(stream);
                IntensityKeys.Add(key);
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

        if (NumFvecKeys > 0)
        {
            stream.Seek(FvecKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumFvecKeys; i++)
            {
                VfxVectorKey key = new();
                key.Read(stream);
                FvecKeys.Add(key);
            }
        }
        
        stream.Seek(endPos, SeekOrigin.Begin);
    }
}