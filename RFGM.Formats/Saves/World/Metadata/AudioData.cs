using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Metadata;

public class AudioData
{
    public MusicFile File;

    public enum MusicFile
    {
        Progression01Day = 0,
        Progression01Night = 1,
        Progression02Day = 2,
        Progression02Night = 3,
        Progression03Day = 4,
        Progression03Night = 5
    }
    
    public void Read(Stream stream)
    {
        File = stream.ReadValueEnum<MusicFile>();
    }
    
    public void Write(Stream stream)
    {
        stream.WriteValueEnum<MusicFile>(File);
    }
}