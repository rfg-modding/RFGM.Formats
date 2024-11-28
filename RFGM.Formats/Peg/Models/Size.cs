namespace RFGM.Formats.Peg.Models;

/// <summary>
/// 2D texture dimensions
/// </summary>
public record Size(int Width, int Height)
{
    public override string ToString()
    {
        return $"({Width}x{Height})";
    }
}
