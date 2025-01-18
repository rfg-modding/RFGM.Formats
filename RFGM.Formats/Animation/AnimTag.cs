using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Animation;

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