using System.Numerics;

namespace RFGM.Formats.Math;

public struct PositionOrient()
{
    public Vector3 Position = Vector3.Zero;
    public Matrix3x3 Orient = Matrix3x3.Identity;
}