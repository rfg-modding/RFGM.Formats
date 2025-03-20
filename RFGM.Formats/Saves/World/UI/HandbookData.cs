using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.UI;

public class HandbookData
{
    public List<HandbookNode> Nodes = new();
    public byte SelectedPage;
    public byte UnlockCounter;

    public struct HandbookNode
    {
        public uint NameCrc;
        public sbyte UnlockNumber;
        public bool Viewed;
    }

    public void Read(Stream stream)
    {
        var nodeCount = stream.ReadUInt8();
        UnlockCounter = stream.ReadUInt8();
        SelectedPage = stream.ReadUInt8();
        
        for (var i = 0; i < nodeCount; i++)
        {
            Nodes.Add(new HandbookNode()
            {
                NameCrc = stream.ReadUInt32(),
                Viewed = stream.ReadBoolean8(),
                UnlockNumber = stream.ReadInt8()
            });
        }
    }


    public void Write(Stream stream)
    {
        stream.WriteUInt8((byte)Nodes.Count);
        stream.WriteUInt8(UnlockCounter);
        stream.WriteUInt8(SelectedPage);

        foreach (var node in Nodes)
        {
            stream.WriteUInt32(node.NameCrc);
            stream.WriteBoolean8(node.Viewed);
            stream.WriteInt8(node.UnlockNumber);
        }
        
        /*for (var i = 0; i < Nodes.Count; i++)
        {
            stream.WriteUInt32(Nodes[i].NameCRC);
            stream.WriteBoolean8(Nodes[i].Viewed);
            stream.WriteInt8(Nodes[i].UnlockNumber);
        }*/
    }
}