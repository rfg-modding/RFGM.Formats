using System.Collections.Immutable;
using System.IO.Abstractions;

namespace RFGM.Formats.Abstractions;

/// <summary>
/// Unified interface to work with formats in abstract manner. Separate from actual format classes to simplify format development: focus on data first, descriptor can be added later
/// </summary>
public interface IFormatDescriptor
{
    Format Format { get; }
    bool CanDecode { get; }
    bool CanEncode { get; }
    bool IsPaired { get; }
    bool IsContainer { get; }
    ImmutableHashSet<string> Extensions { get; }
    bool Match(string name);
    bool Match(IFileSystemInfo fileSystemInfo);
    EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo);
    string GetFileName(EntryInfo data);
    string GetDirectoryName(EntryInfo data);
}
