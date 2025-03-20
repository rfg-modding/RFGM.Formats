using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Player;

public class PlayerInventoryData
{
    public List<InventoryItem> Items = new();

    public class InventoryItem
    {
        public int MagazineSize;
        public int Count;
        public string ItemName = "";
        public int SelectionSlot;
    }

    public void Read(Stream stream)
    {
        var itemCount = stream.ReadInt32();
        for (var i = 0; i < itemCount; i++)
        {
            InventoryItem item = new()
            {
                SelectionSlot = stream.ReadInt32(),
                ItemName = stream.ReadLengthPrefixedString16(),
                Count = stream.ReadInt32(),
                MagazineSize = stream.ReadInt32()
            };

            Items.Add(item);
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteInt32(Items.Count);
        foreach (var t in Items)
        {
            stream.WriteInt32(t.SelectionSlot);
            stream.WriteLengthPrefixedString16(t.ItemName);
            stream.WriteInt32(t.Count);
            stream.WriteInt32(t.MagazineSize);
        }
    }

}