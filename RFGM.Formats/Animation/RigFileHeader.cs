using RFGM.Formats.Streams;

namespace RFGM.Formats.Animation;

public class RigFileHeader
{
    public string Name = String.Empty; //Must not be larger than 31 characters
    public uint Flags;
    public int NumBones;
    public int NumCommonBones;
    public int NumVirtualBones;
    public int NumTags;
    public int BoneNameChecksumsOffset;
    public int BonesOffset;
    public int TagsOffset;

    private const long SizeInFile = 64;

    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;

        //This file only has 32 bytes available for the name
        for (var i = 0; i < 32; i++)
        {
            if (stream.Peek<char>() == '\0')
                break;

            Name += stream.ReadChar8();
        }
        stream.Seek(startPos + 32, SeekOrigin.Begin);
#endif

        Flags = stream.ReadUInt32();
        NumBones = stream.ReadInt32();
        NumCommonBones = stream.ReadInt32();
        NumVirtualBones = stream.ReadInt32();
        NumTags = stream.ReadInt32();
        BoneNameChecksumsOffset = stream.ReadInt32();
        BonesOffset = stream.ReadInt32();
        TagsOffset = stream.ReadInt32();

#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RigFileHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}