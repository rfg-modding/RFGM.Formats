using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Animation;

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