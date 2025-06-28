using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class VfxEmitter
{
    public uint NameOffset;
    public ParticleMaterialData ParticleMaterial = new(); //TODO: Determine if this has valid values in the files or if its just set at runtime
    public VfxEmitterType EmitterType;
    public uint ClassId;
    public bool IsIterative;
    public bool IsReverseRender;
    public bool IsHalfRes;
    public bool IsQuarterRes;
    public bool IsAlwaysOnTop;
    public bool IsSoftRender;
    public float IterationsPerSecond;
    public bool TextureAnimRandomFrames;
    public bool IsEmitNode;
    public bool InheritVelocity;
    public bool LocalSpaceEmission;
    public bool AccelerationEnabled;
    public Vector3 AccelerationStart;
    public Vector3 AccelerationEnd;
    public bool AccelerationVarianceEnabled;
    public Vector3 AccelerationStartVariance;
    public Vector3 AccelerationEndVariance;
    public bool UseColorMidValue;
    public bool IsColorVarianceByValue;
    public bool UseColorSaturationValue;
    public ColorFloat ColorStart;
    public ColorFloat ColorStartVariance;
    public ColorFloat ColorEnd;
    public ColorFloat ColorEndVariance;
    public ColorFloat ColorMidA;
    public ColorFloat ColorMidAVariance;
    public float ColorMidAOffset;
    public ColorFloat ColorMidB;
    public ColorFloat ColorMidBVariance;
    public float ColorMidBOffset;
    public float AlphaMidAOffset;
    public float AlphaMidBOffset;
    public float ColorSaturationStart;
    public float ColorSaturationStartVariance;
    public float ColorSaturationEnd;
    public float ColorSaturationEndVariance;
    public float ColorSaturationMidA;
    public float ColorSaturationMidAVariance;
    public float ColorSaturationMidB;
    public float ColorSaturationMidBVariance;
    public bool UseAdditiveKeyValue;
    public float AdditiveKeyStart;
    public float AdditiveKeyStartVariance;
    public float AdditiveKeyEnd;
    public float AdditiveKeyEndVariance;
    public float AdditiveKeyMidA;
    public float AdditiveKeyMidAVariance;
    public float AdditiveKeyMidB;
    public float AdditiveKeyMidBVariance;
    public uint ColorCorrectionMatrixOffset;
    public int NumTextureAnimRateKeys;
    public uint TextureAnimRateKeysOffset;
    public bool RotationEnabled;
    public float WeightStart;
    public float WeightEnd;
    public float Density;
    public float DensityVariance;
    public int NumEmitNodeEmitters;
    public uint EmitNodeEmittersOffset;
    public int NumTails;
    public ParticleType ParticleType;
    public int MaterialIndex;
    public float YawVariance;
    public float PitchVariance;
    public int MaxParticles;
    public bool UseSizeMidValue;
    public bool UseLengthMidValue;
    public bool UseLength;
    public bool UseLifetimeVariance;
    public float Lifetime;
    public float LifetimeVariance;
    public float SpeedVariance;
    public float AngularSpeedStartVariance;
    public float AngularSpeedEndVariance;
    public bool IsRandomStartRotation;
    public float LodLargeParticleFadeMin;
    public float LodLargeParticleFadeMax;
    public float LodEmissionDistance;
    public float SelfIllum;
    public Vector3 AmbientColor;
    public Vector3 DiffuseColor;
    public float EffectMaterialPercent;
    public float SoftThickness;
    public int NumPositionKeys;
    public uint PositionKeysOffset;
    public int NumRotationKeys;
    public uint RotationKeysOffset;
    public int NumScaleKeys;
    public uint ScaleKeysOffset;
    public int NumEmissionRateKeys;
    public uint EmissionRateKeysOffset;
    public int NumAngularSpeedStartKeys;
    public uint AngularSpeedStartKeysOffset;
    public int NumAngularSpeedEndKeys;
    public uint AngularSpeedEndKeysOffset;
    public int NumSpeedKeys;
    public uint SpeedKeysOffset;
    public int NumSizeStartKeys;
    public uint SizeStartKeysOffset;
    public int NumSizeStartVarianceKeys;
    public uint SizeStartVarianceKeysOffset;
    
    public int NumSizeMidAKeys;
    public uint SizeMidAKeysOffset;
    public int NumSizeMidAVarianceKeys;
    public uint SizeMidAVarianceKeysOffset;
    public int NumSizeMidAOffsetKeys;
    public uint SizeMidAOffsetKeysOffset;
    
    public int NumSizeMidBKeys;
    public uint SizeMidBKeysOffset;
    public int NumSizeMidBVarianceKeys;
    public uint SizeMidBVarianceKeysOffset;
    public int NumSizeMidBOffsetKeys;
    public uint SizeMidBOffsetKeysOffset;
    
    public int NumSizeEndKeys;
    public uint SizeEndKeysOffset;
    public int NumSizeEndVarianceKeys;
    public uint SizeEndVarianceKeysOffset;
    
    public int NumLengthStartKeys;
    public uint LengthStartKeysOffset;
    public int NumLengthStartVarianceKeys;
    public uint LengthStartVarianceKeysOffset;
    public int NumLengthMidAKeys;
    public uint LengthMidAKeysOffset;
    public int NumLengthMidAVarianceKeys;
    public uint LengthMidAVarianceKeysOffset;
    public int NumLengthMidAOffsetKeys;
    public uint LengthMidAOffsetKeysOffset;
    
    public int NumLengthMidBKeys;
    public uint LengthMidBKeysOffset;
    public int NumLengthMidBVarianceKeys;
    public uint LengthMidBVarianceKeysOffset;
    public int NumLengthMidBOffsetKeys;
    public uint LengthMidBOffsetKeysOffset;
    
    public int NumLengthEndKeys;
    public uint LengthEndKeysOffset;
    public int NumLengthEndVarianceKeys;
    public uint LengthEndVarianceKeysOffset;

    public uint ParticleSystemEmitterOffset; //Note: This is 0 in all vanilla files. Likely set by the game at runtime.
    public int ParentIndex;
    public int UniqueDataSize;
    public uint UniqueDataOffset;

    public string Name = string.Empty;
    public Matrix3x3 ColorCorrectionMatrix = Matrix3x3.Identity;
    public List<VfxFloatKey> TextureAnimRateKeys = new();
    public List<VfxVectorKey> PositionKeys = new();
    public List<VfxQuaternionKey> RotationKeys = new();
    public List<VfxVectorKey> ScaleKeys = new();
    public List<VfxFloatKey> EmissionRateKeys = new();
    
    public List<VfxFloatKey> AngularSpeedStartKeys = new();
    public List<VfxFloatKey> AngularSpeedEndKeys = new();
    public List<VfxFloatKey> SpeedKeys = new();
    
    public List<VfxFloatKey> SizeStartKeys = new();
    public List<VfxFloatKey> SizeStartVarianceKeys = new();
    
    public List<VfxFloatKey> SizeMidAKeys = new();
    public List<VfxFloatKey> SizeMidAVarianceKeys = new();
    public List<VfxFloatKey> SizeMidAOffsetKeys = new();
    
    public List<VfxFloatKey> SizeMidBKeys = new();
    public List<VfxFloatKey> SizeMidBVarianceKeys = new();
    public List<VfxFloatKey> SizeMidBOffsetKeys = new();
    
    public List<VfxFloatKey> SizeEndKeys = new();
    public List<VfxFloatKey> SizeEndVarianceKeys = new();
    
    public List<VfxFloatKey> LengthStartKeys = new();
    public List<VfxFloatKey> LengthStartVarianceKeys = new();

    public List<VfxFloatKey> LengthMidAKeys = new();
    public List<VfxFloatKey> LengthMidAVarianceKeys = new();
    public List<VfxFloatKey> LengthMidAOffsetKeys = new();
    
    public List<VfxFloatKey> LengthMidBKeys = new();
    public List<VfxFloatKey> LengthMidBVarianceKeys = new();
    public List<VfxFloatKey> LengthMidBOffsetKeys = new();
    
    public List<VfxFloatKey> LengthEndKeys = new();
    public List<VfxFloatKey> LengthEndVarianceKeys = new();
    
    public byte[] UniqueData = [];
    
    private const long SizeInFile = 968;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        NameOffset = stream.ReadUInt32();
        stream.Skip(4);

        ParticleMaterial.Read(stream);
        EmitterType = (VfxEmitterType)stream.ReadInt32();
        ClassId = stream.ReadUInt32();
        IsIterative = stream.ReadBoolean();
        IsReverseRender = stream.ReadBoolean();
        IsHalfRes = stream.ReadBoolean();
        IsQuarterRes = stream.ReadBoolean();
        IsAlwaysOnTop = stream.ReadBoolean();
        IsSoftRender = stream.ReadBoolean();
        stream.Skip(2); //Padding

        IterationsPerSecond = stream.ReadFloat();
        TextureAnimRandomFrames = stream.ReadBoolean();
        IsEmitNode = stream.ReadBoolean();
        InheritVelocity = stream.ReadBoolean();
        LocalSpaceEmission = stream.ReadBoolean();
        AccelerationEnabled = stream.ReadBoolean();
        stream.Skip(3); //Padding

        AccelerationStart = stream.ReadStruct<Vector3>();
        AccelerationEnd = stream.ReadStruct<Vector3>();
        AccelerationVarianceEnabled = stream.ReadBoolean();
        stream.Skip(3); //Padding
        
        AccelerationStartVariance = stream.ReadStruct<Vector3>();
        AccelerationEndVariance = stream.ReadStruct<Vector3>();
        UseColorMidValue = stream.ReadBoolean();
        IsColorVarianceByValue = stream.ReadBoolean();
        UseColorSaturationValue = stream.ReadBoolean();
        stream.Skip(1); //Padding
        
        ColorStart = stream.ReadStruct<ColorFloat>();
        ColorStartVariance = stream.ReadStruct<ColorFloat>();
        ColorEnd = stream.ReadStruct<ColorFloat>();
        ColorEndVariance = stream.ReadStruct<ColorFloat>();
        ColorMidA = stream.ReadStruct<ColorFloat>();
        ColorMidAVariance = stream.ReadStruct<ColorFloat>();
        ColorMidAOffset = stream.ReadFloat();
        ColorMidB = stream.ReadStruct<ColorFloat>();
        ColorMidBVariance = stream.ReadStruct<ColorFloat>();
        ColorMidBOffset = stream.ReadFloat();
        AlphaMidAOffset = stream.ReadFloat();
        AlphaMidBOffset = stream.ReadFloat();
        ColorSaturationStart = stream.ReadFloat();
        ColorSaturationStartVariance = stream.ReadFloat();
        ColorSaturationEnd = stream.ReadFloat();
        ColorSaturationEndVariance = stream.ReadFloat();
        ColorSaturationMidA = stream.ReadFloat();
        ColorSaturationMidAVariance = stream.ReadFloat();
        ColorSaturationMidB = stream.ReadFloat();
        ColorSaturationMidBVariance = stream.ReadFloat();
        UseAdditiveKeyValue = stream.ReadBoolean();
        stream.Skip(3); //Padding

        AdditiveKeyStart = stream.ReadFloat();
        AdditiveKeyStartVariance = stream.ReadFloat();
        AdditiveKeyEnd = stream.ReadFloat();
        AdditiveKeyEndVariance = stream.ReadFloat();
        AdditiveKeyMidA = stream.ReadFloat();
        AdditiveKeyMidAVariance = stream.ReadFloat();
        AdditiveKeyMidB = stream.ReadFloat();
        AdditiveKeyMidBVariance = stream.ReadFloat();
        
        ColorCorrectionMatrixOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumTextureAnimRateKeys = stream.ReadInt32();
        stream.Skip(4); //Padding
        TextureAnimRateKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        RotationEnabled = stream.ReadBoolean();
        stream.Skip(3);
        
        WeightStart = stream.ReadFloat();
        WeightEnd = stream.ReadFloat();
        Density = stream.ReadFloat();
        DensityVariance = stream.ReadFloat();
        NumEmitNodeEmitters = stream.ReadInt32();
        EmitNodeEmittersOffset = stream.ReadUInt32();
        stream.Skip(4);
        NumTails = stream.ReadInt32();
        ParticleType = (ParticleType)stream.ReadInt32();
        MaterialIndex = stream.ReadInt32();
        YawVariance = stream.ReadFloat();
        PitchVariance = stream.ReadFloat();
        MaxParticles = stream.ReadInt32();
        UseSizeMidValue = stream.ReadBoolean();
        UseLengthMidValue = stream.ReadBoolean();
        UseLength = stream.ReadBoolean();
        UseLifetimeVariance = stream.ReadBoolean();
        Lifetime = stream.ReadFloat();
        LifetimeVariance = stream.ReadFloat();
        SpeedVariance = stream.ReadFloat();
        AngularSpeedStartVariance = stream.ReadFloat();
        AngularSpeedEndVariance = stream.ReadFloat();
        IsRandomStartRotation = stream.ReadBoolean();
        stream.Skip(3);
        
        LodLargeParticleFadeMin = stream.ReadFloat();
        LodLargeParticleFadeMax = stream.ReadFloat();
        LodEmissionDistance = stream.ReadFloat();
        SelfIllum = stream.ReadFloat();
        AmbientColor = stream.ReadStruct<Vector3>();
        DiffuseColor = stream.ReadStruct<Vector3>();
        EffectMaterialPercent = stream.ReadFloat();
        SoftThickness = stream.ReadFloat();
        stream.Skip(4); //Padding
        
        NumPositionKeys = stream.ReadInt32();
        stream.Skip(4);
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
        
        NumEmissionRateKeys = stream.ReadInt32();
        stream.Skip(4);
        EmissionRateKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumAngularSpeedStartKeys = stream.ReadInt32();
        stream.Skip(4);
        NumAngularSpeedEndKeys = stream.ReadInt32();
        stream.Skip(4);
        
        NumAngularSpeedEndKeys = stream.ReadInt32();
        stream.Skip(4);
        AngularSpeedEndKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSpeedKeys = stream.ReadInt32();
        stream.Skip(4);
        SpeedKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeStartKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeStartKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeStartVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeStartVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeMidAKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeMidAKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeMidAVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeMidAVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeMidAOffsetKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeMidAOffsetKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeMidBKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeMidBKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeMidBVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeMidBVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeMidBOffsetKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeMidBOffsetKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeEndKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeEndKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumSizeEndVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        SizeEndVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthStartKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthStartKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthStartVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthStartVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthMidAKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthMidAKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthMidAVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthMidAVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthMidAOffsetKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthMidAOffsetKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthMidBKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthMidBKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthMidBVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthMidBVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthMidBOffsetKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthMidBOffsetKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthEndKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthEndKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        NumLengthEndVarianceKeys = stream.ReadInt32();
        stream.Skip(4);
        LengthEndVarianceKeysOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        ParticleSystemEmitterOffset = stream.ReadUInt32();
        stream.Skip(4);

        ParentIndex = stream.ReadInt32();
        UniqueDataSize = stream.ReadInt32();
        UniqueDataOffset = stream.ReadUInt32();
        stream.Skip(4);
        
        var endPos = stream.Position;
#if DEBUG
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxEmitter. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif

        if (NameOffset != uint.MaxValue)
        {
            stream.Seek(NameOffset, SeekOrigin.Begin);
            Name = stream.ReadAsciiNullTerminatedString();
        }

        if (ColorCorrectionMatrixOffset != uint.MaxValue)
        {
            stream.Seek(ColorCorrectionMatrixOffset, SeekOrigin.Begin);
            ColorCorrectionMatrix = stream.ReadStruct<Matrix3x3>();
        }

        TextureAnimRateKeys = TryReadFloatKeys(stream, TextureAnimRateKeysOffset, NumTextureAnimRateKeys);
        
        //TODO: Determine if the data at EmitNodeEmittersOffset has any purpose. Seems to be a runtime type that's based on the data in this class. So likely useless until runtime.

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
                VfxQuaternionKey key = new VfxQuaternionKey();
                key.Read(stream);
                RotationKeys.Add(key);
            }
        }

        if (NumScaleKeys > 0)
        {
            stream.Seek(ScaleKeysOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumScaleKeys; i++)
            {
                VfxVectorKey key = new VfxVectorKey();
                key.Read(stream);
                ScaleKeys.Add(key);
            }
        }

        EmissionRateKeys = TryReadFloatKeys(stream, EmissionRateKeysOffset, NumEmissionRateKeys);
        AngularSpeedStartKeys = TryReadFloatKeys(stream, AngularSpeedStartKeysOffset, NumAngularSpeedStartKeys);
        AngularSpeedEndKeys = TryReadFloatKeys(stream, AngularSpeedEndKeysOffset, NumAngularSpeedEndKeys);
        SpeedKeys = TryReadFloatKeys(stream, SpeedKeysOffset, NumSpeedKeys);
        SizeStartKeys = TryReadFloatKeys(stream, SizeStartKeysOffset, NumSizeStartKeys);
        SizeStartVarianceKeys = TryReadFloatKeys(stream, SizeStartVarianceKeysOffset, NumSizeStartVarianceKeys);
        SizeMidAKeys = TryReadFloatKeys(stream, SizeMidAKeysOffset, NumSizeMidAKeys);
        SizeMidAVarianceKeys = TryReadFloatKeys(stream, SizeMidAVarianceKeysOffset, NumSizeMidAVarianceKeys);
        SizeMidAOffsetKeys = TryReadFloatKeys(stream, SizeMidAOffsetKeysOffset, NumSizeMidAOffsetKeys);
        SizeMidBKeys = TryReadFloatKeys(stream, SizeMidBKeysOffset, NumSizeMidBKeys);
        SizeMidBVarianceKeys = TryReadFloatKeys(stream, SizeMidBVarianceKeysOffset, NumSizeMidBVarianceKeys);
        SizeMidBOffsetKeys = TryReadFloatKeys(stream, SizeMidBOffsetKeysOffset, NumSizeMidBOffsetKeys);
        SizeEndKeys = TryReadFloatKeys(stream, SizeEndKeysOffset, NumSizeEndKeys);
        SizeEndVarianceKeys = TryReadFloatKeys(stream, SizeEndVarianceKeysOffset, NumSizeEndVarianceKeys);
        LengthStartKeys = TryReadFloatKeys(stream, LengthStartKeysOffset, NumLengthStartKeys);
        LengthStartVarianceKeys = TryReadFloatKeys(stream, LengthStartVarianceKeysOffset, NumLengthStartVarianceKeys);
        LengthMidAKeys = TryReadFloatKeys(stream, LengthMidAKeysOffset, NumLengthMidAKeys);
        LengthMidAVarianceKeys = TryReadFloatKeys(stream, LengthMidAVarianceKeysOffset, NumLengthMidAVarianceKeys);
        LengthMidAOffsetKeys = TryReadFloatKeys(stream, LengthMidAOffsetKeysOffset, NumLengthMidAOffsetKeys);
        LengthMidBKeys = TryReadFloatKeys(stream, LengthMidBKeysOffset, NumLengthMidBKeys);
        LengthMidBVarianceKeys = TryReadFloatKeys(stream, LengthMidBVarianceKeysOffset, NumLengthMidBVarianceKeys);
        LengthMidBOffsetKeys = TryReadFloatKeys(stream, LengthMidBOffsetKeysOffset, NumLengthMidBOffsetKeys);
        LengthEndKeys = TryReadFloatKeys(stream, LengthEndKeysOffset, NumLengthEndKeys);
        LengthEndVarianceKeys = TryReadFloatKeys(stream, LengthEndVarianceKeysOffset, NumLengthEndVarianceKeys);

        if (UniqueDataOffset > 0 && UniqueDataOffset != uint.MaxValue && UniqueDataSize > 0)
        {
            //TODO: Figure out what this data is
            stream.Seek(UniqueDataOffset, SeekOrigin.Begin);
            UniqueData = new byte[UniqueDataSize];
            stream.ReadExactly(UniqueData);
        }
        
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