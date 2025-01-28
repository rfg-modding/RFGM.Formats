using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
    public string ToCsv()
    {
        var props = GetType()
            .GetProperties()
            .Select(x => new {x.Name, Value = x.GetValue(this) ?? string.Empty})
            .OrderBy(x => x.Name)
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

    /// <summary>entry index</summary>
    public IntString? Index { get; set; }

    /// <summary>container entry count</summary>
    [JsonIgnore]
    public IntString? Entries { get; set; }

    /// <summary>PEG container alignment</summary>
    public IntString? PegAlign { get; set; }

    /// <summary>VPP/STR2 compresison mode</summary>
    public RfgVpp.HeaderBlock.Mode? VppMode { get; set; }

    /// <summary>texture mip count</summary>
    public IntString? TexMips { get; set; }

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

    /// <summary>
    /// Wrapper around int to be readable from JSON string. required to survive broken json used for property serialization
    /// </summary>
    public record IntString(int Value)
    {
        public static implicit operator int(IntString x) => x.Value;
        public static implicit operator IntString(int x) => new(x);
        public override string ToString() => Value.ToString();
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, JsonOptions)
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
        return JsonSerializer.Deserialize<Properties>(kindaJson, JsonOptions)!;
    }

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new SizeConverter(),
            new IntStringConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        },
    };

    public class SizeConverter : JsonConverter<Size>
    {
        public override Size Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => Size.Parse(reader.GetString())!;

        public override void Write(
            Utf8JsonWriter writer,
            Size value,
            JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }

    public class IntStringConverter : JsonConverter<IntString>
    {
        public override IntString Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => new(int.Parse(reader.GetString()!));

        public override void Write(
            Utf8JsonWriter writer,
            IntString value,
            JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value.ToString());
    }
}
