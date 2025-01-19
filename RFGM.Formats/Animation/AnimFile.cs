using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Animation;

//Reads .anim_pc files for RFGR
public class AnimFile(string name)
{
    public string Name = name;
    
    public uint Signature;
    public byte Version;
    public byte Flags;
    public ushort EndTock;
    public byte RampIn;
    public byte RampOut;
    public byte NumBones;
    public byte NumRigBones;
    public Quaternion TotalRotation;
    public Vector3 TotalTranslation;
    public int AnimRigToBoneMappingOffset;
    public int RootControllerBoneOffset;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long headerStartPos = stream.Position;        
#endif
        
        Signature = stream.ReadUInt32();
        Version = stream.ReadUInt8();
        Flags = stream.ReadUInt8();
        EndTock = stream.ReadUInt16();
        RampIn = stream.ReadUInt8();
        RampOut = stream.ReadUInt8();
        NumBones = stream.ReadUInt8();
        NumRigBones = stream.ReadUInt8();
        TotalRotation = stream.ReadStruct<Quaternion>();
        TotalTranslation = stream.ReadStruct<Vector3>();
        AnimRigToBoneMappingOffset = stream.ReadInt32();
        RootControllerBoneOffset = stream.ReadInt32();
        
#if DEBUG
        long headerEndPos = stream.Position;
        long headerBytesRead = headerEndPos - headerStartPos;
        if (headerBytesRead != 48)
        {
            throw new Exception($"Invalid size for AnimFile header. Expected {48} bytes, read {headerBytesRead} bytes");
        }
#endif
        
        //TODO: Figure out what's in the rest of the file
    }
}

