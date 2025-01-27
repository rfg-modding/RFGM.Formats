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

        for (var i = 0; i < Header.NumBones; i++)
        {
            BoneChecksums.Add(stream.ReadUInt32());
        }

        for (var i = 0; i < Header.NumBones; i++)
        {
            AnimBone bone = new();
            bone.Read(stream);
            Bones.Add(bone);
        }

        for (var i = 0; i < Header.NumTags; i++)
        {
            AnimTag tag = new();
            tag.Read(stream);
            Tags.Add(tag);
        }

        var namesBaseOffset = stream.Position;
        foreach (var bone in Bones)
        {
            stream.Seek(namesBaseOffset + bone.NameOffset, SeekOrigin.Begin);
            bone.Name = stream.ReadAsciiNullTerminatedString();
        }
        foreach (var tag in Tags)
        {
            stream.Seek(namesBaseOffset + tag.NameOffset, SeekOrigin.Begin);
            tag.Name = stream.ReadAsciiNullTerminatedString();
        }
    }
}