using System.Numerics;

namespace RFGM.Formats.Math;

//Meant to match the matrix43 type used by RFG
public struct Matrix4x3
{
    public Matrix3x3 Rotation;
    public Vector3 Translation;
}