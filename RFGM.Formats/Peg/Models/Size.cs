namespace RFGM.Formats.Peg.Models;

/// <summary>
/// 2D texture dimensions
/// </summary>
public record Size(int Width, int Height)
{
    public override string ToString()
    {
        return $"{Width}x{Height}";
    }

    public static readonly Size Zero = new (0, 0);

    public static Size? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var tokens = value.Split('x');
        if (tokens.Length != 2)
        {
            throw new ArgumentException($"Can not parse Size from string [{value}]");
        }

        var width = int.Parse(tokens[0]);
        var height = int.Parse(tokens[1]);
        return new Size(width, height);
    }
}
