using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;

namespace RFGM.Formats;

public static class FormatUtils
{
    public static async Task<string> ComputeHash(IFileInfo file, CancellationToken token)
    {
        await using var s = file.OpenRead();
        return await ComputeHash(s, token);
    }

    public static async Task<string> ComputeHash(PairedFiles pairedFiles, CancellationToken token)
    {
        var cpuHash = await ComputeHash(pairedFiles.Cpu, token);
        var gpuHash = await ComputeHash(pairedFiles.Gpu, token);
        return $"{cpuHash}_{gpuHash}";
    }

    public static async Task<string> ComputeHash(PairedStreams pairedStreams, CancellationToken token)
    {
        var cpuHash = await ComputeHash(pairedStreams.Cpu, token);
        var gpuHash = await ComputeHash(pairedStreams.Gpu, token);
        return $"{cpuHash}_{gpuHash}";
    }

    /// <summary>
    /// Expects stream at position 0. Rewinds stream to 0. Does not close stream.
    /// </summary>
    public static async Task<string> ComputeHash(Stream s, CancellationToken token)
    {
        if (!s.CanSeek)
        {
            throw new ArgumentException($"Need seekable stream, got {s}", nameof(s));
        }

        if (!s.CanRead)
        {
            throw new ArgumentException($"Need readable stream, got {s}", nameof(s));
        }

        if (s.Position != 0)
        {
            throw new ArgumentException($"Expected start of stream, got position = {s.Position}", nameof(s));
        }

        using var sha = SHA256.Create();
        var hashValue = await sha.ComputeHashAsync(s, token);
        var hash = BitConverter.ToString(hashValue).Replace("-", "");
        s.Seek(0, SeekOrigin.Begin);
        return hash;
    }

    /// <summary>
    /// Clones chain of wrappers for multithreaded file access with inflation and whatever
    /// </summary>
    public static Stream MakeDeepOwnCopy(this Stream value)
    {
        return value switch
        {
            FileSystemStream fs when fs.GetType().Name == "MockFileStream" => CopyToMs(fs), // for tests
            FileSystemStream fs =>  new FileStream(fs.Name, FileMode.Open, FileAccess.Read, FileShare.Read),
            FileStream f => new FileStream(f.Name, FileMode.Open, FileAccess.Read, FileShare.Read),
            StreamView sv => new StreamView(MakeDeepOwnCopy(sv.UnderlyingStream), sv.ViewStart, sv.Length, sv.Name + "+") {IsStreamOwner = true},
            SeekableInflaterStream si => new SeekableInflaterStream(MakeDeepOwnCopy(si.UnderlyingStream), si.Length, si.Name) {IsStreamOwner = true},
            MemoryStream ms => new StreamView(ms, 0, ms.Length, "fake MemoryStream copy"){IsStreamOwner = false}, // dont ever copy memory, just wrap it in thread-safe manner
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Only view, inflater and file streams are supported")
        };

        Stream CopyToMs(Stream s)
        {
            var view = new StreamView(s, 0, s.Length, "tmp mock view");
            var ms = new MemoryStream();
            view.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }

    /// <summary>
    /// Clones chain of wrappers for multithreaded file access with inflation and whatever
    /// </summary>
    public static PairedStreams MakeDeepOwnCopy(this PairedStreams value)
    {
        return new PairedStreams(MakeDeepOwnCopy(value.Cpu), MakeDeepOwnCopy(value.Gpu));
    }

    public static string StreamToString(Stream value) =>
        value switch
        {
            FileStream f => $"FileSteram {f.Name} {GetValueSafe(() => f.Position)} {GetValueSafe(() => f.Length)}",
            FileSystemStream f => $"FileSystemSteram {f.Name} {GetValueSafe(() => f.Position)} {GetValueSafe(() => f.Length)}",
            {} s => s.ToString()!
        };

    private static long GetValueSafe(Func<long> action)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            return -1;
        }
    }

    public static async Task DisposeNotNull(IAsyncDisposable? value)
    {
        if (value is null)
        {
            return;
        }

        await value.DisposeAsync();
    }

    /// <summary>
    /// Enter subdirectory without creating it
    /// </summary>
    public static IDirectoryInfo Descend(this IDirectoryInfo parent, string dirName)
    {
        var path = parent.FileSystem.Path.Combine(parent.FullName, dirName);
        return parent.FileSystem.DirectoryInfo.New(path);
    }

    public static string GetLastExtension(string name)
    {
        var firstDot = name.LastIndexOf('.');
        return firstDot == -1
            ? string.Empty
            : name[firstDot..];
    }

    /// <summary>
    /// Splits "name.foo.bar" to "name" + ".foo.bar"
    /// </summary>
    public static (string name, string lastExt) GetNameAndLastExt(string input)
    {
        var ext = GetLastExtension(input);
        var name = input[..^ext.Length];
        return (name, ext);
    }

    /// <summary>
    /// Writes xmldoc without declaration to a memory stream. Stream is kept open and rewound to begin
    /// </summary>
    public static void SerializeToMemoryStream(XmlDocument document, MemoryStream ms, bool indent = false)
    {
        using (var tw = XmlWriter.Create(ms,
                   new XmlWriterSettings
                   {
                       CloseOutput = false,
                       Indent = indent, // NOTE: some files cant be reformatted or even minimized, game crashes if you do that
                       Encoding = Utf8NoBom,
                       OmitXmlDeclaration = true
                   }))
        {
            document.WriteTo(tw);
        }

        ms.Seek(0, SeekOrigin.Begin);
    }

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);


    public static readonly JsonSerializerOptions JsonOptions = new()
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

    public static readonly JsonSerializerOptions UserFriendlyJsonOptions = new(JsonOptions)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Wrapper around int to be readable from JSON string. required to survive broken json used for property serialization
    /// </summary>
    public record IntString(int Value)
    {
        public static implicit operator int(IntString x) => x.Value;
        public static implicit operator IntString(int x) => new(x);
        public override string ToString() => Value.ToString();
    }

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
