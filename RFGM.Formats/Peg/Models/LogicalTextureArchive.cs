namespace RFGM.Formats.Peg.Models;

public record LogicalTextureArchive(
    IEnumerable<LogicalTexture> LogicalTextures,
    string Name,
    int TotalLength,
    int DataLength,
    int Align
)
{
    public string UnpackName => $"{Name}.{Align}";
}
