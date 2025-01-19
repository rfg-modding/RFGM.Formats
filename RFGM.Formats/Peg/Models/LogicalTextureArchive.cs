namespace RFGM.Formats.Peg.Models;

public record LogicalTextureArchive(
    List<LogicalTexture> LogicalTextures,
    string Name,
    int TotalLength,
    int DataLength,
    int Align
);
