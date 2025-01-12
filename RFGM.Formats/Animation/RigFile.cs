using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Animation;

//For rig_pc files
public class RigFile
{
    public string Name;
    public RigFileHeader Header = new();
    public List<uint> BoneChecksums = new();
    public List<AnimBone> Bones = new();
    public List<AnimTag> Tags = new();

    public RigFile(string name)
    {
        Name = name;
    }
    
    public void Read(Stream stream)
    {
        Header.Read(stream);
        if (Header.BoneNameChecksumsOffset != 0 || Header.BonesOffset != 0 || Header.TagsOffset != 0)
        {
            //Note: All rigs examined so far have zero for these. If a non zero offset is encountered I want it to fail so I can manually verify that the offset works as expected
            throw new Exception("Unexpected non zero offset in .rig_pc file when 0 is expected.");
        }

        for (int i = 0; i < Header.NumBones; i++)
        {
            BoneChecksums.Add(stream.ReadUInt32());
        }
        
        for (int i = 0; i < Header.NumBones; i++)
        {
            AnimBone bone = new();
            bone.Read(stream);
            Bones.Add(bone);
        }

        for (int i = 0; i < Header.NumTags; i++)
        {
            AnimTag tag = new();
            tag.Read(stream);
            Tags.Add(tag);
        }
        
        long namesBaseOffset = stream.Position;
        foreach (AnimBone bone in Bones)
        {
            stream.Seek(namesBaseOffset + bone.NameOffset, SeekOrigin.Begin);
            bone.Name = stream.ReadAsciiNullTerminatedString();
        }
        foreach (AnimTag tag in Tags)
        {
            stream.Seek(namesBaseOffset + tag.NameOffset, SeekOrigin.Begin);
            tag.Name = stream.ReadAsciiNullTerminatedString();
        }
    }
}

public class RigFileHeader()
{
    public string Name = String.Empty; //Must not be larger than 31 characters
    public uint Flags = 0;
    public int NumBones = 0;
    public int NumCommonBones = 0;
    public int NumVirtualBones = 0;
    public int NumTags = 0;
    public int BoneNameChecksumsOffset = 0;
    public int BonesOffset = 0;
    public int TagsOffset = 0;

    private const long SizeInFile = 64;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        //This file only has 32 bytes available for the name
        for (int i = 0; i < 32; i++)
        {
            if (stream.Peek<char>() == '\0')
                break;
            
            Name += stream.ReadChar8();
        }
        stream.Seek(startPos + 32, SeekOrigin.Begin);
        
        Flags = stream.ReadUInt32();
        NumBones = stream.ReadInt32();
        NumCommonBones = stream.ReadInt32();
        NumVirtualBones = stream.ReadInt32();
        NumTags = stream.ReadInt32();
        BoneNameChecksumsOffset = stream.ReadInt32();
        BonesOffset = stream.ReadInt32();
        TagsOffset = stream.ReadInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RigFileHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public class AnimBone
{
    public int NameOffset;
    public Vector3 InvTranslation;
    public Vector3 RelBoneTranslation;
    public int ParentIndex;
    public int Vid;

    public string Name;
    
    private const long SizeInFile = 36;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        NameOffset = stream.ReadInt32();
        InvTranslation = stream.ReadStruct<Vector3>();
        RelBoneTranslation = stream.ReadStruct<Vector3>();
        ParentIndex = stream.ReadInt32();
        Vid = stream.ReadInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for AnimBone. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public class AnimTag
{
    public int NameOffset;
    public Matrix3x3 Rotation;
    public Vector3 Translation;
    public int ParentIndex;
    public int Vid;
    
    public string Name;
    
    private const long SizeInFile = 60;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        NameOffset = stream.ReadInt32();
        Rotation = stream.ReadStruct<Matrix3x3>();
        Translation = stream.ReadStruct<Vector3>();
        ParentIndex = stream.ReadInt32();
        Vid = stream.ReadInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for AnimTag. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}