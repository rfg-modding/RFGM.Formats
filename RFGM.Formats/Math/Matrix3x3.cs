using System.Numerics;

namespace RFGM.Formats.Math;

//Note: Only added for reading from zone files. Convert it to System.Numerics.Matrix4x4 or something similar for a real math type
public struct Matrix3x3
{
    //TODO: Figure out if this is the correct order to match RFG
    public float M11;
    public float M12;
    public float M13;

    public float M21;
    public float M22;
    public float M23;

    public float M31;
    public float M32;
    public float M33;

    public static Matrix3x3 Identity => new Matrix3x3
    (
        1.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f,
        0.0f, 0.0f, 1.0f
    ); 
    
    public Matrix3x3(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M31 = m31;
        M32 = m32;
        M33 = m33;
    }

    public Matrix3x3()
    {
        this = Identity;
    }
    
    public Matrix4x4 ToMatrix4x4()
    {
        return new Matrix4x4
        (
            M11,  M12,  M13,  0.0f,
            M21,  M22,  M23,  0.0f,
            M31,  M32,  M33,  0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
    }
}