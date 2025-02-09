namespace RFGM.Archiver.Models;

public record NestedItem<T>(Breadcrumbs Breadcrumbs, T Item)
{
    public override string ToString() => $"[{Breadcrumbs}] {Item}";
}
