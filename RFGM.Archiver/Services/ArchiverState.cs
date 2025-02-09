using System.Collections.Concurrent;
using System.IO.Abstractions;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Db;

namespace RFGM.Archiver.Services;

public class ArchiverState(IFileSystem fileSystem)
{
    public readonly ConcurrentBag<DbPrimitive> PrimitiveIndex = new();
    public readonly ConcurrentBag<DbFile> FileIndex = new();
    public readonly ConcurrentBag<PrimitiveLocation> PrimitiveLocations = new();

    private readonly ConcurrentBag<Metadata> metadata = new();

    private readonly ConcurrentBag<string> destinations = new();

    public void StoreMetadata(Metadata value) => metadata.Add(value);

    public void RememberDestination(string value) => destinations.Add(value);

    public List<Metadata> GetMetadata() => metadata.ToList();

    public List<IDirectoryInfo> GetDestinations()
    {
        return destinations
            .Distinct()
            .Select(x => fileSystem.DirectoryInfo.New(x))
            .Where(x => x.Exists)
            .OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
