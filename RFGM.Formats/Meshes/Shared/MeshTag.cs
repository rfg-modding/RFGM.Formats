using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Shared;

public struct MeshTag()
{
    public uint NameCrc = 0;
    //NOTE: This is really a 3x3 matrix. Matrix4x4 is being using since System.Numerics doesn't have a 3x3 matrix
    public Matrix4x4 Rotation = Matrix4x4.Identity;
    public Vector3 Translation = default;

    public void Read(Stream stream)
    {
        NameCrc = stream.ReadUInt32();
        //TODO: Make sure these are being read in the correct order
        Rotation[0, 0] = stream.ReadFloat();
        Rotation[1, 0] = stream.ReadFloat();
        Rotation[2, 0] = stream.ReadFloat();
        Rotation[0, 1] = stream.ReadFloat();
        Rotation[1, 1] = stream.ReadFloat();
        Rotation[2, 1] = stream.ReadFloat();
        Rotation[0, 2] = stream.ReadFloat();
        Rotation[1, 2] = stream.ReadFloat();
        Rotation[2, 2] = stream.ReadFloat();
        Translation = stream.ReadStruct<Vector3>();
    }
}