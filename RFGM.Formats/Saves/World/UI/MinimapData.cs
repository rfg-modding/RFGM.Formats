using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.UI;

public class MinimapData
{
    public List<uint> VisibleOverFowList = new();
    public float ZoomRadius;

    public void Read(Stream stream)
    {
        ZoomRadius = stream.ReadFloat();
        var count = stream.ReadUInt32();
        for (var i = 0; i < count; i++)
        {
            VisibleOverFowList.Add(stream.ReadUInt32());
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteFloat(ZoomRadius);
        stream.WriteUInt32((uint)VisibleOverFowList.Count);
        foreach (var t in VisibleOverFowList)
        {
            stream.WriteUInt32(t);
        }
    }
}