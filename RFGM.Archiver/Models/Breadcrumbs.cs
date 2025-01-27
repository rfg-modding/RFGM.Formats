namespace RFGM.Archiver.Models;

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
