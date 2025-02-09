namespace RFGM.Archiver.Models;

public class Breadcrumbs
{
    private Breadcrumbs(IEnumerable<string> path) => this.path = path.ToList();

    private readonly IReadOnlyList<string> path;

    public Breadcrumbs Descend(string container) => new(path.Append(container.ToLowerInvariant()));

    public static Breadcrumbs Init() => new([]);

    public override string ToString() => string.Join("/", path);

    public IReadOnlyList<string> ToList() => path;
}
