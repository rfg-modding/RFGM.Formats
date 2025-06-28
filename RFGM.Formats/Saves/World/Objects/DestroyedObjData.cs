using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Objects;

public class DestroyedObjData
{
    public List<uint> DestroyedObjects = new();

    public void Read(Stream stream)
    {
        var count = stream.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            DestroyedObjects.Add(stream.ReadUInt32());
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteInt32(DestroyedObjects.Count);
        foreach (var t in DestroyedObjects)
        {
            stream.WriteUInt32(t);
        }
    }
}