using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using RFGM.Formats;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Models;

public record PackSettings(
    bool Metadata,
    bool Force
)
{
    public static readonly PackSettings Default = new(false, false);

    public override string? ToString() => this == Default ? "Default" : Serialize();

    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append($"{nameof(Metadata)}={Metadata}");
        sb.Append($"{nameof(Force)}={Force}");
        return sb.ToString();
    }
}
