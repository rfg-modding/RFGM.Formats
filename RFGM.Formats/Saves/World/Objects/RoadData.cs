using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Objects;

public class RoadData
{
    public List<RoadObject> Roads = new();

    public class RoadObject
    {
        public uint Handle;
        public List<byte> Data = new();
    }

    public void Read(Stream stream)
    {
        var roadCount = stream.ReadInt32();

        for (var i = 0; i < roadCount; i++)
        {
            var road = new RoadObject
            {
                Handle = stream.ReadUInt32()
            };

            var dataCount = stream.ReadUInt8();
            for (var j = 0; j < dataCount; j++)
            {
                road.Data.Add(stream.ReadUInt8());
            }

            Roads.Add(road);
        }
    }

    public void Write(Stream stream)
    {
        var roadCount = Roads.Count(r => r.Data.Count > 0);
        stream.WriteInt32(roadCount);

        foreach (var road in Roads.Where(road => road.Data.Count > 0))
        {
            stream.WriteUInt32(road.Handle);
            stream.WriteUInt8((byte)road.Data.Count);
            foreach (var b in road.Data)
            {
                stream.WriteUInt8(b);
            }
        }
    }

}