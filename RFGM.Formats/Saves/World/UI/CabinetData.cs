using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.UI;

public class CabinetData
{
    public List<WeaponListItem> Weapons = new();

    public class WeaponListItem
    {
        public uint Id;
        public bool Unlocked;
    }

    public void Read(Stream stream)
    {
        var weaponCount = stream.ReadUInt8();

        for (var i = 0; i < weaponCount; ++i)
        {
            Weapons.Add(new WeaponListItem()
            {
                Id = stream.ReadUInt32(),
                Unlocked = stream.ReadBoolean8()
            });
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt8((byte)Weapons.Count);
        foreach (var t in Weapons)
        {
            stream.WriteUInt32(t.Id);
            stream.WriteBoolean8(t.Unlocked);
        }
    }
}