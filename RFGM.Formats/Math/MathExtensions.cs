using System.Numerics;

namespace RFGM.Formats.Math;

public static class MathExtensions
{
    public static Matrix3x3 ToMatrix3x3(this Matrix4x4 m)
    {
        var mat3x3 = new Matrix3x3();
        mat3x3.M11 = m.M11;
        mat3x3.M12 = m.M12;
        mat3x3.M13 = m.M13;
        mat3x3.M21 = m.M21;
        mat3x3.M22 = m.M22;
        mat3x3.M23 = m.M23;
        mat3x3.M31 = m.M31;
        mat3x3.M32 = m.M32;
        mat3x3.M33 = m.M33;
        return mat3x3;
    }
}