namespace RFGM.Archiver.Models;

public record VppEntry(string Name, string RelativePath, int Order, uint Offset, long Length, uint CompressedSize, string Hash) : IMetadata
{
    public override string ToString() => $"{RelativePath} {nameof(Order)}={Order} {nameof(Offset)}={Offset} {nameof(Length)}={Length} " +
                                         $"{nameof(CompressedSize)}={CompressedSize} {nameof(Hash)}={Hash}";
}

public class Breadcrumbs
{
    private Breadcrumbs(IEnumerable<string> path)
    {
        this.path = path.ToList();
    }

    private readonly IReadOnlyList<string> path;

    public Breadcrumbs Descend(string container)
    {
        return new Breadcrumbs(path.Append(container));
    }

    public static Breadcrumbs Init()
    {
        return new Breadcrumbs([]);
    }

    public override string ToString()
    {
        return string.Join("/", path);
    }
}
