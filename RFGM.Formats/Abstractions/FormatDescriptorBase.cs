using System.Collections.Immutable;
using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RFGM.Formats.Peg.Models;

namespace RFGM.Formats.Abstractions;

public abstract class FormatDescriptorBase : IFormatDescriptor
{
    protected FormatDescriptorBase()
    {
        Name = GetType().Name.Replace("Descriptor", "");
        CanDecodeExtensions = CanDecodeExt.ToImmutableHashSet();
        CanEncodeExtensions = CanEncodeExt.ToImmutableHashSet();
        CanDecode = CanDecodeExtensions.Any();
        CanEncode = CanEncodeExtensions.Any();
    }

    public virtual bool CanDecode { get; }
    public virtual bool CanEncode { get; }
    public string Name { get; }
    public ImmutableHashSet<string> CanDecodeExtensions { get; }
    public ImmutableHashSet<string> CanEncodeExtensions { get; }

    public bool DecodeMatch(string name)
    {
        if (!CanDecode)
        {
            return false;
        }

        return DecodeMatchInternal(name);
    }

    public bool EncodeMatch(IFileSystemInfo fileSystemInfo)
    {
        if (!CanEncode)
        {
            return false;
        }

        return EncodeMatchInternal(fileSystemInfo);
    }

    public string GetDecodeName(EntryInfo data, bool writeProperties)
    {
        if (!CanDecode)
        {
            throw new InvalidOperationException($"{Name}: Decode is not supported");
        }

        return GetDecodeNameInternal(data, writeProperties);
    }

    public string GetEncodeName(EntryInfo data)
    {
        if (!CanEncode)
        {
            throw new InvalidOperationException($"{Name}: Encode is not supported");
        }

        return data.Name;
    }

    public EntryInfo FromFileSystem(IFileSystemInfo fileSystemInfo)
    {
        if (!CanDecode)
        {
            throw new InvalidOperationException($"{Name}: Decode is not supported");
        }

        if (IsContainer && fileSystemInfo is not IDirectoryInfo)
        {
            throw new InvalidOperationException($"{Name}: Expected directory for container format");
        }

        if (!IsContainer && fileSystemInfo is not IFileInfo)
        {
            throw new InvalidOperationException($"{Name}: Expected file for non-container format");
        }

        return FromFileSystemInternal(fileSystemInfo);
    }

    public abstract bool IsPaired { get; }
    public abstract bool IsContainer { get; }
    protected abstract List<string> CanDecodeExt { get; }
    protected abstract List<string> CanEncodeExt { get; }
    protected virtual bool DecodeMatchInternal(string name) => CanDecodeExtensions.Contains(FormatUtils.GetFullExtension(name));
    protected virtual bool EncodeMatchInternal(IFileSystemInfo fileSystemInfo) => CanEncodeExtensions.Contains(FormatUtils.GetFullExtension(fileSystemInfo.Name));

    protected virtual EntryInfo FromFileSystemInternal(IFileSystemInfo fileSystemInfo)
    {
        var match = PropertyNameFormat.Match(fileSystemInfo.Name);
        if (!match.Success)
        {
            throw new ArgumentException($"{Name}: Invalid name [{fileSystemInfo.FullName}]");
        }

        var name = match.Groups["nameNoExt"].Value;
        ArgumentNullException.ThrowIfNull(name);
        var props = Properties.Deserialize(match.Groups["props"].Value);
        ArgumentNullException.ThrowIfNull(props);
        var originalExt = match.Groups["ext"].Value;
        var newExt = GetEncodeExt(originalExt);
        return new EntryInfo($"{name}{newExt}", this, props);
    }

    protected virtual string GetDecodeNameInternal(EntryInfo data, bool encodeProperties)
    {
        var (name, originalExt) = FormatUtils.GetNameAndFullExt(data.Name);
        var newExt = GetDecodeExt(originalExt);
        var props = encodeProperties ?
            " " + data.Properties.Serialize()
            : string.Empty;
        return $"{name}{props}{newExt}";
    }

    protected virtual string GetDecodeExt(string originalExt)
    {
        if (CanDecodeExtensions.Count != 1 || CanEncodeExtensions.Count != 1)
        {
            throw new InvalidOperationException($"{Name}: Default implementation can't choose from multiple extensions");
        }

        // file we're reading should be the one we're supposed to encode later
        if (originalExt != CanDecodeExtensions.Single())
        {
            throw new InvalidOperationException($"{Name}: Default implementation expects known extension");
        }
        return CanEncodeExtensions.Single();
    }

    protected virtual string GetEncodeExt(string originalExt)
    {
        if (CanDecodeExtensions.Count != 1 || CanEncodeExtensions.Count != 1)
        {
            throw new InvalidOperationException($"{Name}: Default implementation can't choose from multiple extensions");
        }

        // file we're reading should be the one we decoded earlier
        if (originalExt != CanEncodeExtensions.Single())
        {
            throw new InvalidOperationException($"{Name}: Default implementation expects known extension");
        }
        return CanDecodeExtensions.Single();
    }



    public static readonly Regex PropertyNameFormat = new(@"^(?<nameNoExt>.*?)\s*(?<props>{.*})?(?<ext>\..*)?$", RegexOptions.Compiled);

}
