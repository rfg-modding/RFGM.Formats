using System.Numerics;

namespace RFGM.Formats.Math;

public struct Ray(Vector3 start, Vector3 end)
{
    public Vector3 Start = start;
    public Vector3 End = end;
}