using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using RFGM.Formats;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Models;

public record UnpackSettings(
    Matcher Matcher,
    ImageFormat ImageFormat,
    OptimizeFor OptimizeFor,
    bool SkipContainers,
    bool WriteProperties,
    bool XmlFormat,
    bool Recursive,
    bool Metadata,
    bool Force
)
{
    public static readonly Matcher DefaultMatcher = new Matcher(StringComparison.OrdinalIgnoreCase).AddInclude("**/*");

    public static readonly Matcher EmptyMatcher = new();

    public static readonly UnpackSettings Default = new(DefaultMatcher, ImageFormat.png, OptimizeFor.speed, false, true, false, true, false, false);

    public static readonly UnpackSettings Meta = new(EmptyMatcher, ImageFormat.raw, OptimizeFor.speed, false, false, false, true, true, false);

    public override string? ToString() => this switch
    {
        { } x when x == Default => "Default",
        { } x when x == Meta => "Meta",
        _ => Serialize()
    };
    public string Serialize()
    {
        var sb = new StringBuilder();
        var m = Matcher switch
        {
            { } x when x == DefaultMatcher => "Default",
            { } x when x == EmptyMatcher => "Empty",
            _ => "Custom"
        };
        sb.Append($"{nameof(Matcher)}={m},");
        sb.Append($"{nameof(ImageFormat)}={ImageFormat},");
        sb.Append($"{nameof(OptimizeFor)}={OptimizeFor},");
        sb.Append($"{nameof(SkipContainers)}={SkipContainers},");
        sb.Append($"{nameof(WriteProperties)}={WriteProperties},");
        sb.Append($"{nameof(XmlFormat)}={XmlFormat},");
        sb.Append($"{nameof(Recursive)}={Recursive},");
        sb.Append($"{nameof(Metadata)}={Metadata},");
        sb.Append($"{nameof(Force)}={Force}");
        return sb.ToString();
    }
}
