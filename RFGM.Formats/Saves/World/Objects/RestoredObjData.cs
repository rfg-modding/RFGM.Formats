using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Objects;

public class RestoredObjData
{
    public List<uint> PendingRestorations = new();
    public List<uint> RestoredObjects = new();

    public void Read(Stream stream)
    {
        var pendingRestorationsCount = stream.ReadUInt16();
        for (var i = 0; i < pendingRestorationsCount; i++)
        {
            PendingRestorations.Add(stream.ReadUInt32());
        }

        var restoredObjectsCount = stream.ReadUInt16();
        for (var i = 0; i < restoredObjectsCount; i++)
        {
            RestoredObjects.Add(stream.ReadUInt32());
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt16((ushort)PendingRestorations.Count);
        foreach (var t in PendingRestorations)
        {
            stream.WriteUInt32(t);
        }

        stream.WriteUInt16((ushort)RestoredObjects.Count);
        foreach (var t in RestoredObjects)
        {
            stream.WriteUInt32(t);
        }
    }
}