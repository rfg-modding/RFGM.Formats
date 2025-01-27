using System.Collections.Immutable;

namespace RFGM.Formats.Abstractions;

public class Properties
{
    private readonly object locker = new();

    private readonly Dictionary<string, object> store = new();

    public void Add(string key, object value)
    {
        lock (locker)
        {
            store.Add(key, value);
        }
    }

    public T Get<T>(string key)
    {
        lock (locker)
        {
            return (T) store[key];
        }
    }

    public T GetOrDefault<T>(string key, T defaultValue)
    {
        lock (locker)
        {
            if (store.TryGetValue(key, out var value))
            {
                return (T) value;
            }

            return defaultValue;
        }
    }

    public override string ToString()
    {
        lock (locker)
        {
            return string.Join(", ", store.ToList().Select(x => $"{x.Key}=[{x.Value}]"));
        }
    }

    public string ToCsv()
    {
        lock (locker)
        {
            return string.Join(",", AllKeys.Order().Select(x => store.GetValueOrDefault(x, "")));
        }
    }

    public static string ToCsvHeader() => string.Join(",", AllKeys.Order());

    // peg
    public const string Align = "peg.align";
    // vpp
    public const string Mode = "vpp.mode";
    // file
    public const string Index = "entry.index";
    // conainer
    public const string Entries = "container.entries";
    // texture
    public const string Format = "texture.format";
    public const string Flags = "texture.flags";
    public const string MipLevels = "texture.mipLevels";
    public const string ImageFormat = "texture.imageFormat";
    public const string Size = "texture.size";
    public const string Source = "texture.source";

    public static readonly ImmutableHashSet<string> AllKeys = new HashSet<string>
    {
        Align,
        Mode,
        Index,
        Entries,
        Format,
        Flags,
        MipLevels,
        ImageFormat,
        Size,
        Source
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
}
