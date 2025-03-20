using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.UI;

public class TooltipData
{
    public byte MaxTooltips;
    public byte[] TimesDisplayed = new byte[128];

    public void Read(Stream stream)
    {
        MaxTooltips = stream.ReadUInt8();
        for (var i = 0; i < MaxTooltips; ++i)
        {
            TimesDisplayed[i] = stream.ReadUInt8();
        }
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt8(MaxTooltips);
        for (var i = 0; i < MaxTooltips; ++i)
        {
            stream.WriteUInt8(TimesDisplayed[i]);
        }
    }
}