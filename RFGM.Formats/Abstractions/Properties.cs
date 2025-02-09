using System.Text.Json;
using System.Text.Json.Serialization;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Abstractions;

/// <summary>
/// Entry attributes serialized to/from file names. Field names must be short. All types must be readable from json string (quoted)
/// </summary>
public record Properties
{
    /// <summary>entry index</summary>
    public FormatUtils.IntString? Index { get; set; }

    /// <summary>container entry count</summary>
    [JsonIgnore]
    public FormatUtils.IntString? Entries { get; set; }

    /// <summary>PEG container alignment</summary>
    public FormatUtils.IntString? PegAlign { get; set; }

    /// <summary>VPP/STR2 compresison mode</summary>
    public RfgVpp.HeaderBlock.Mode? VppMode { get; set; }

    /// <summary>texture mip count</summary>
    public FormatUtils.IntString? TexMips { get; set; }

    /// <summary>texture format</summary>
    public RfgCpeg.Entry.BitmapFormat? TexFmt { get; set; }

    /// <summary>texture flags</summary>
    public TextureFlags? TexFlags { get; set; }

    /// <summary>texture dimensions</summary>
    public Size? TexSize { get; set; }

    /// <summary>texture source dimensions</summary>
    public Size? TexSrc { get; set; }

    /// <summary>image format for conversion</summary>
    [JsonIgnore]
    public ImageFormat? ImgFmt { get; set; }

    public string ToCsv()
    {
        var props = GetType()
            .GetProperties()
            .Select(x => new {x.Name, Value = x.GetValue(this) ?? string.Empty})
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Value);
        return string.Join(",", props);
    }

    public static string ToCsvHeader()
    {
        var props = typeof(Properties)
            .GetProperties()
            .Select(x => x.Name)
            .Order();
        return string.Join(",", props);
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, FormatUtils.JsonOptions)
            .Replace("\"","")
            .Replace(":","=")
            .Replace(",", ", ");
    }

    public static Properties Deserialize(string value)
    {
        var kindaJson = string.IsNullOrWhiteSpace(value)
                ? "{}"
                : value
                    .Replace("{", "{\"")
                    .Replace("}", "\"}")
                    .Replace("=", "\":\"")
                    .Replace(", ", "\",\"")
            ;
        return JsonSerializer.Deserialize<Properties>(kindaJson, FormatUtils.JsonOptions)!;
    }
}
