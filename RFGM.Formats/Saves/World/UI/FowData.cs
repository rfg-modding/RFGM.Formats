using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.UI;

public class FowData
{
    public byte[] BitArray = new byte[32768];
    public int EndMarker;

    public void Read(Stream stream)
    {
        stream.ReadUInt16();
        stream.ReadUInt16();
        stream.ReadUInt32();
        stream.ReadUInt32();
        stream.ReadUInt32();
            
        for (var i = 0; i < BitArray.Length; i++)
        {
            BitArray[i] = stream.ReadUInt8();
        }

        EndMarker = stream.ReadInt32();
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt16(16);
        stream.WriteUInt16(1024);
        stream.WriteUInt32(262144);
        stream.WriteUInt32(262144);
        stream.WriteUInt32(32768);
        stream.Write(BitArray, 0, BitArray.Length);
        stream.WriteInt32(EndMarker);
    }
}