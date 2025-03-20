using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Objects;

public class RestorableObjData
{
    public List<uint> Objects = new();
    public List<RestoreListObject> RestoreList = new();

    public class RestoreListObject
    {
        public int ObjectCount;
        public uint ObjectHandle;
        public uint ObjListIndex;
    }

    public void Read(Stream stream)
    {
        var restorableObjectsCount = stream.ReadUInt32();
        for (var i = 0; i < restorableObjectsCount; i++)
        {
            Objects.Add(stream.ReadUInt32());
        }

        var restoreListCount = stream.ReadUInt32();
        for (var i = 0; i < restoreListCount; i++)
        {
            RestoreList.Add(new RestoreListObject
            {
                ObjectHandle = stream.ReadUInt32(),
                ObjectCount = stream.ReadInt32(),
                ObjListIndex = stream.ReadUInt32()
            });
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32((uint)Objects.Count);
        foreach (var t in Objects)
        {
            stream.WriteUInt32(t);
        }

        stream.WriteUInt32((uint)RestoreList.Count);
        foreach (var t in RestoreList)
        {
            stream.WriteUInt32(t.ObjectHandle);
            stream.WriteInt32(t.ObjectCount);
            stream.WriteUInt32(t.ObjListIndex);
        }
    }
}