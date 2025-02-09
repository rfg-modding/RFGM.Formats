using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class VfxCorona
{
    public uint NameOffset;
    public ParticleMaterialData ParticleMaterial = new();
    public int Type;

    public uint RotationOffsetKeysOffset;
    public int NumRotationOffsetKeys;

    public int NumPositionKeys;
    public bool IsLinearScaling;
    public bool IsLinearFading;
    
    public uint PositionKeysOffset;
    
    public int NumRotationKeys;
    public uint RotationKeysOffset;

    public int NumScaleKeys;
    public uint ScaleKeysOffset;

    public int NumRadiusStartKeys;
    public uint RadiusStartKeysOffset;
    
    public int NumRadiusEndKeys;
    public uint RadiusEndKeysOffset;

    public int NumWidthScaleKeys;
    public uint WidthScaleKeysOffset;

    public int NumVisibilityRadiusKeys;
    public uint VisibilityRadiusKeysOffset;

    public int NumColorStartKeys;
    public uint ColorStartKeysOffset;

    public int NumColorEndKeys;
    public uint ColorEndKeysOffset;
    
    public int NumPositionOffsetKeys;
    public uint PositionOffsetKeysOffset;

    public int NumSaturationKeys;
    public uint SaturationKeysOffset;

    public int NumRotationScaleKeys;
    public uint RotationScaleKeysOffset;

    public int NumIntensityScaleKeys;
    public uint IntensityScaleKeysOffset;

    public int NumIntensityExponentialDecayKeys;
    public uint IntensityExponentialDecayKeysOffset;
    
    public int NumOpacityExponentialDecayKeys;
    public uint OpacityExponentialDecayKeysOffset;

    public int NumRadiusDistanceStartKeys;
    public uint RadiusDistanceStartKeysOffset;
    
    public int NumRadiusDistanceEndKeys;
    public uint RadiusDistanceEndKeysOffset;
    
    public int NumColorDistanceStartKeys;
    public uint ColorDistanceStartKeysOffset;
    
    public int NumColorDistanceEndKeys;
    public uint ColorDistanceEndKeysOffset;
    
    public int NumRadiusExponentialDecayKeys;
    public uint RadiusExponentialDecayKeysOffset;

    public int NumViewFadeScaleKeys;
    public uint ViewFadeScaleKeysOffset;
    
    public int NumViewFadeDecayKeys;
    public uint ViewFadeDecayKeysOffset;
    
    public int NumOpacityVisibilityDecayKeys;
    public uint OpacityVisibilityDecayKeysOffset;
    
    public int NumRadiusVisibilityDecayKeys;
    public uint RadiusVisibilityDecayKeysOffset;

    public bool ViewFadeIntensityEnabled;
    public bool ViewFadeOpacityEnabled;
    public bool ViewFadeRadiusEnabled;
    
    public string Name = string.Empty;
    public List<VfxFloatKey> RotationOffsetKeys = new();
    public List<VfxVectorKey> PositionKeys = new();
    public List<VfxQuaternionKey> RotationKeys = new();
    public List<VfxVectorKey> ScaleKeys = new();
    public List<VfxFloatKey> RadiusStartKeys = new();
    public List<VfxFloatKey> RadiusEndKeys = new();
    public List<VfxFloatKey> WidthScaleKeys = new();
    public List<VfxFloatKey> VisibilityRadiusKeys = new();
    public List<VfxVector4Key> ColorStartKeys = new();
    public List<VfxVector4Key> ColorEndKeys = new();
    public List<VfxFloatKey> PositionOffsetKeys = new();
    public List<VfxFloatKey> SaturationKeys = new();
    public List<VfxFloatKey> RotationScaleKeys = new();
    public List<VfxFloatKey> IntensityScaleKeys = new();
    public List<VfxFloatKey> IntensityExponentialDecayKeys = new();
    public List<VfxFloatKey> OpacityExponentialDecayKeys = new();
    public List<VfxFloatKey> RadiusDistanceStartKeys = new();
    public List<VfxFloatKey> RadiusDistanceEndKeys = new();
    public List<VfxFloatKey> ColorDistanceStartKeys = new();
    public List<VfxFloatKey> ColorDistanceEndKeys = new();
    public List<VfxFloatKey> RadiusExponentialDecayKeys = new();
    public List<VfxFloatKey> ViewFadeScaleKeys = new();
    public List<VfxFloatKey> ViewFadeDecayKeys = new();
    public List<VfxFloatKey> OpacityVisibilityDecayKeys = new();
    public List<VfxFloatKey> RadiusVisibilityDecayKeys = new();
    
    private const long SizeInFile = 472;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        NameOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        ParticleMaterial.Read(stream);
        Type = stream.ReadInt32();
        stream.Skip(4);
        
        RotationOffsetKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        NumRotationOffsetKeys = stream.ReadInt32();
        
        NumPositionKeys = stream.ReadInt32();
        IsLinearScaling = stream.ReadBoolean();
        IsLinearFading = stream.ReadBoolean();
        stream.Skip(6);
        
        PositionKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRotationKeys = stream.ReadInt32();
        stream.Skip(4);
        RotationKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumScaleKeys = stream.ReadInt32();
        stream.Skip(4);
        ScaleKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRadiusStartKeys = stream.ReadInt32();
        stream.Skip(4);
        RadiusStartKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRadiusEndKeys = stream.ReadInt32();
        stream.Skip(4);
        RadiusEndKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumWidthScaleKeys = stream.ReadInt32();
        stream.Skip(4);
        WidthScaleKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumVisibilityRadiusKeys = stream.ReadInt32();
        stream.Skip(4);
        VisibilityRadiusKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumColorStartKeys = stream.ReadInt32();
        stream.Skip(4);
        ColorStartKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumColorEndKeys = stream.ReadInt32();
        stream.Skip(4);
        ColorEndKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumPositionOffsetKeys = stream.ReadInt32();
        stream.Skip(4);
        PositionOffsetKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSaturationKeys = stream.ReadInt32();
        stream.Skip(4);
        SaturationKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRotationScaleKeys = stream.ReadInt32();
        stream.Skip(4);
        RotationScaleKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumIntensityScaleKeys = stream.ReadInt32();
        stream.Skip(4);
        IntensityScaleKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumIntensityExponentialDecayKeys = stream.ReadInt32();
        stream.Skip(4);
        IntensityExponentialDecayKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumOpacityExponentialDecayKeys = stream.ReadInt32();
        stream.Skip(4);
        OpacityExponentialDecayKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRadiusDistanceStartKeys = stream.ReadInt32();
        stream.Skip(4);
        RadiusDistanceStartKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRadiusDistanceEndKeys = stream.ReadInt32();
        stream.Skip(4);
        RadiusDistanceEndKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumColorDistanceStartKeys = stream.ReadInt32();
        stream.Skip(4);
        ColorDistanceStartKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumColorDistanceEndKeys = stream.ReadInt32();
        stream.Skip(4);
        ColorDistanceEndKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRadiusExponentialDecayKeys = stream.ReadInt32();
        stream.Skip(4);
        RadiusExponentialDecayKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumViewFadeScaleKeys = stream.ReadInt32();
        stream.Skip(4);
        ViewFadeScaleKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumViewFadeDecayKeys = stream.ReadInt32();
        stream.Skip(4);
        ViewFadeDecayKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumOpacityVisibilityDecayKeys = stream.ReadInt32();
        stream.Skip(4);
        OpacityVisibilityDecayKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumRadiusVisibilityDecayKeys = stream.ReadInt32();
        stream.Skip(4);
        RadiusVisibilityDecayKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        ViewFadeIntensityEnabled = stream.ReadBoolean();
        ViewFadeOpacityEnabled = stream.ReadBoolean();
        ViewFadeRadiusEnabled = stream.ReadBoolean();
        stream.Skip(5);
        
        var endPos = stream.Position;
#if DEBUG
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxCorona. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif

        if (NameOffset != uint.MaxValue)
        {
            stream.Seek(NameOffset, SeekOrigin.Begin);
            Name = stream.ReadAsciiNullTerminatedString();
        }
        
        RotationOffsetKeys = TryReadFloatKeys(stream, RotationOffsetKeysOffset, NumRotationOffsetKeys);

        if (NumPositionKeys > 0)
        {
            stream.Seek(PositionKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumPositionKeys; i++)
            {
                VfxVectorKey key = new VfxVectorKey();
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

        if (NumScaleKeys > 0)
        {
            stream.Seek(ScaleKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumScaleKeys; i++)
            {
                VfxVectorKey key = new();
                key.Read(stream);
                ScaleKeys.Add(key);
            }
        }
        
        RadiusStartKeys = TryReadFloatKeys(stream, RadiusStartKeysOffset, NumRadiusStartKeys);
        RadiusEndKeys = TryReadFloatKeys(stream, RadiusEndKeysOffset, NumRadiusEndKeys);
        WidthScaleKeys = TryReadFloatKeys(stream, WidthScaleKeysOffset, NumWidthScaleKeys);
        VisibilityRadiusKeys = TryReadFloatKeys(stream, VisibilityRadiusKeysOffset, NumVisibilityRadiusKeys);

        if (NumColorStartKeys > 0)
        {
            stream.Seek(ColorStartKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumColorStartKeys; i++)
            {
                VfxVector4Key key = new();
                key.Read(stream);
                ColorStartKeys.Add(key);
            }
        }
        
        if (NumColorEndKeys > 0)
        {
            stream.Seek(ColorEndKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumColorEndKeys; i++)
            {
                VfxVector4Key key = new();
                key.Read(stream);
                ColorEndKeys.Add(key);
            }
        }
        
        PositionOffsetKeys = TryReadFloatKeys(stream, PositionOffsetKeysOffset, NumPositionOffsetKeys);
        SaturationKeys = TryReadFloatKeys(stream, SaturationKeysOffset, NumSaturationKeys);
        RotationScaleKeys = TryReadFloatKeys(stream, RotationScaleKeysOffset, NumRotationScaleKeys);
        IntensityScaleKeys = TryReadFloatKeys(stream, IntensityScaleKeysOffset, NumIntensityScaleKeys);
        IntensityExponentialDecayKeys = TryReadFloatKeys(stream, IntensityExponentialDecayKeysOffset, NumIntensityExponentialDecayKeys);
        OpacityExponentialDecayKeys = TryReadFloatKeys(stream, OpacityExponentialDecayKeysOffset, NumOpacityExponentialDecayKeys);
        RadiusDistanceStartKeys = TryReadFloatKeys(stream, RadiusDistanceStartKeysOffset, NumRadiusDistanceStartKeys);
        RadiusDistanceEndKeys = TryReadFloatKeys(stream, RadiusDistanceEndKeysOffset, NumRadiusDistanceEndKeys);
        ColorDistanceStartKeys = TryReadFloatKeys(stream, ColorDistanceStartKeysOffset, NumColorDistanceStartKeys);
        ColorDistanceEndKeys = TryReadFloatKeys(stream, ColorDistanceEndKeysOffset, NumColorDistanceEndKeys);
        RadiusExponentialDecayKeys = TryReadFloatKeys(stream, RadiusExponentialDecayKeysOffset, NumRadiusExponentialDecayKeys);
        ViewFadeScaleKeys = TryReadFloatKeys(stream, ViewFadeScaleKeysOffset, NumViewFadeScaleKeys);
        ViewFadeDecayKeys = TryReadFloatKeys(stream, ViewFadeDecayKeysOffset, NumViewFadeDecayKeys);
        OpacityVisibilityDecayKeys = TryReadFloatKeys(stream, OpacityVisibilityDecayKeysOffset, NumOpacityVisibilityDecayKeys);
        RadiusVisibilityDecayKeys = TryReadFloatKeys(stream, RadiusVisibilityDecayKeysOffset, NumRadiusVisibilityDecayKeys);
        
        stream.Seek(endPos, SeekOrigin.Begin);
    }
    
    private List<VfxFloatKey> TryReadFloatKeys(Stream stream, uint offset, int numKeys)
    {
        if (numKeys > 0 && offset != uint.MaxValue)
        {
            List<VfxFloatKey> keys = new List<VfxFloatKey>();
            for (int i = 0; i < numKeys; i++)
            {
                VfxFloatKey key = new VfxFloatKey();
                key.Read(stream);
                keys.Add(key);
            }
            
            return keys;
        }
        else
        {
            return [];
        }
    }
}