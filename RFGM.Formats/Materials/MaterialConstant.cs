using RFGM.Formats.Streams;

namespace RFGM.Formats.Materials;

public struct MaterialConstant
{
    public readonly float[] Constants = new float[4];

    public MaterialConstant()
    {
        
    }

    public void Read(Stream stream)
    {
        Constants[0] = stream.ReadFloat();
        Constants[1] = stream.ReadFloat();
        Constants[2] = stream.ReadFloat();
        Constants[3] = stream.ReadFloat();
    }
}