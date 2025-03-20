namespace RFGM.Formats.Saves.World.UI;

public class UiData
{
    public FowData FogOfWar = new();
    public HandbookData Handbook = new();
    public MinimapData Minimap = new();
    public TooltipData Tooltips = new();
    public CabinetData WeaponCabinet = new();

    public void Read(Stream stream)
    {
        FogOfWar.Read(stream);
        Minimap.Read(stream);
        Tooltips.Read(stream);
        Handbook.Read(stream);
        WeaponCabinet.Read(stream);
    }

    public void Write(Stream stream)
    {
        FogOfWar.Write(stream);
        Minimap.Write(stream);
        Tooltips.Write(stream);
        Handbook.Write(stream);
        WeaponCabinet.Write(stream);
    }
}