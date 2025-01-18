using RFGM.Formats.Streams;

namespace RFGM.Formats.Animation;

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

        //This file only has 32 bytes available for the name
        for (int i = 0; i < 32; i++)
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
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RigFileHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}