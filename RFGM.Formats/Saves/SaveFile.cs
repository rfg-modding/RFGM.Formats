using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves;

public class SaveFile
{
    public const int SaveFileSizeBytes = 31479964;
    public const int SaveOptionsSizeBytes = 2404;
    public const int SlotCount = 12;
    
    public SaveHeader Header = new();
    public List<SaveSlot> Slots = new();
    public byte[] Options = [];
    public SaveProfile Profile = new();

    public void Read(Stream stream)
    {
        Header.Read(stream);
        Slots.Clear();
        
        for (var i = 0; i < SlotCount; i++)
        {
            SaveSlot saveSlot = new();
            saveSlot.Read(stream);
            Slots.Add(saveSlot);
        }

        Options = stream.ReadBytes(SaveOptionsSizeBytes);
        Profile.Read(stream);
    }

    public void Write(Stream stream)
    {
        var saveFileBuffer = new byte[SaveFileSizeBytes];
        using (var ms = new MemoryStream())
        {
            Header.Checksum = 0;
            Header.Write(ms);

            foreach (var slot in Slots)
            {
                slot.Write(ms);
            }
            
            ms.WriteBytes(Options);
            Profile.Write(ms);

            ms.ToArray().CopyTo(saveFileBuffer, 0);
        }

        Header.Checksum = Hash.HashVolitionCRCAlt(saveFileBuffer);
        BitConverter.GetBytes(Header.Checksum).CopyTo(saveFileBuffer, 4);
        stream.WriteBytes(saveFileBuffer);
    }
}