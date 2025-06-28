using System.Numerics;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Player;

public class PlayerPositionData
{
    public bool Died;
    public Matrix4x4 Orientation;
    public Vector3 Position;
    public Matrix4x4 SafehouseOrientation;
    public Vector3 SafehousePosition;

    public void Read(Stream stream)
    {
        Died = stream.ReadBoolean();

        Position = stream.ReadStruct<Vector3>();
        Orientation[0, 0] = stream.ReadFloat();
        Orientation[1, 0] = stream.ReadFloat();
        Orientation[2, 0] = stream.ReadFloat();
        Orientation[0, 1] = stream.ReadFloat();
        Orientation[1, 1] = stream.ReadFloat();
        Orientation[2, 1] = stream.ReadFloat();
        Orientation[0, 2] = stream.ReadFloat();
        Orientation[1, 2] = stream.ReadFloat();
        Orientation[2, 2] = stream.ReadFloat();

        SafehousePosition = stream.ReadStruct<Vector3>();
        SafehouseOrientation[0, 0] = stream.ReadFloat();
        SafehouseOrientation[1, 0] = stream.ReadFloat();
        SafehouseOrientation[2, 0] = stream.ReadFloat();
        SafehouseOrientation[0, 1] = stream.ReadFloat();
        SafehouseOrientation[1, 1] = stream.ReadFloat();
        SafehouseOrientation[2, 1] = stream.ReadFloat();
        SafehouseOrientation[0, 2] = stream.ReadFloat();
        SafehouseOrientation[1, 2] = stream.ReadFloat();
        SafehouseOrientation[2, 2] = stream.ReadFloat();
    }

    public void Write(Stream stream)
    {
        stream.WriteBoolean(Died);

        stream.WriteStruct(Position);
        stream.WriteFloat(Orientation[0, 0]);
        stream.WriteFloat(Orientation[1, 0]);
        stream.WriteFloat(Orientation[2, 0]);
        stream.WriteFloat(Orientation[0, 1]);
        stream.WriteFloat(Orientation[1, 1]);
        stream.WriteFloat(Orientation[2, 1]);
        stream.WriteFloat(Orientation[0, 2]);
        stream.WriteFloat(Orientation[1, 2]);
        stream.WriteFloat(Orientation[2, 2]);

        stream.WriteStruct(SafehousePosition);
        stream.WriteFloat(SafehouseOrientation[0, 0]);
        stream.WriteFloat(SafehouseOrientation[1, 0]);
        stream.WriteFloat(SafehouseOrientation[2, 0]);
        stream.WriteFloat(SafehouseOrientation[0, 1]);
        stream.WriteFloat(SafehouseOrientation[1, 1]);
        stream.WriteFloat(SafehouseOrientation[2, 1]);
        stream.WriteFloat(SafehouseOrientation[0, 2]);
        stream.WriteFloat(SafehouseOrientation[1, 2]);
        stream.WriteFloat(SafehouseOrientation[2, 2]);
    }
}