using System.Collections.Immutable;
using System.IO.Abstractions;

namespace RFGM.Formats.Abstractions;

/// <summary>
/// Unified interface to work with formats in abstract manner. Separate from actual format classes to simplify format development: focus on data first, descriptor can be added later
/// </summary>
public interface IFormatDescriptor
{
    bool CanDecode { get; }
    bool CanEncode { get; }
    bool IsPaired { get; }
    bool IsContainer { get; }
    string Name { get; }
    ImmutableHashSet<string> CanDecodeExtensions { get; }
    ImmutableHashSet<string> CanEncodeExtensions { get; }
    bool DecodeMatch(string name);
    bool EncodeMatch(IFileSystemInfo fileSystemInfo);
    string GetDecodeName(EntryInfo data, bool writeProperties);
    string GetEncodeName(EntryInfo data);
    EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo);
}
