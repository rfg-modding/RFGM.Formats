using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using RFGM.Formats;

namespace RFGM.Archiver.Models.Db;

public class PrimitiveLocation
{
    public string Id { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? File { get; init; }
    public string? Message { get; init; }
}


[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
public class PrimitiveEntry
{
    /// <summary>
    /// Path starting from vpp, including container and name
    /// </summary>
    public string Id { get; init; } = null!;
    public string ContainerId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string PrimitiveType { get; init; } = null!;
    public string PrimitiveFlags { get; init; } = null!;
    public string Allocator { get; init; } = null!;
    public long SplitExtIndex { get; init; }
    public long CpuSize { get; init; }
    public long GpuSize { get; init; }
    public bool IncludeCpuSize { get; init; }
    public bool IncludeGpuSize { get; init; }
}

[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
public class ContainerEntry
{
    /// <summary>
    /// Path starting from vpp, including name
    /// </summary>
    public string Id { get; init; } = null!;
    public string AsmId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ContainerType { get; init; } = null!;
    public string ContainerFlags { get; init; } = null!;
    public long DataOffset { get; init; }
    public long CompressedDataLength { get; init; }
    public long SizesCount { get; init; }
    public long PrimitiveCount { get; init; }
    public string Primitives { get; init; } = null!;
    public bool IsVirtual { get; init; }
}

[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
public class MetadataEntry
{
    public MetadataEntry()
    {
    }

    public MetadataEntry(Models.Metadata x)
    {
        Name = x.Name;
        Path = x.Path;
        Format = x.Format;
        IsContainer = x.IsContainer;
        Hash = x.Hash;
        Length = x.Length;
        Properties = JsonSerializer.Serialize(x.Properties, FormatUtils.JsonOptions);
    }

    /// <summary>
    /// Autoincrement index purely for SQL reasons
    /// </summary>
    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public string Path { get; init; } = null!;
    public string Format { get; init; } = null!;
    public bool IsContainer { get; init; }
    public string Hash { get; init; } = null!;
    public long Length { get; init; }
    public string Properties { get; init; } = null!;
}

[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
public class FileEntry
{
    /// <summary>
    /// Autoincrement index purely for SQL reasons
    /// </summary>
    public string Id { get; init; } = null!;
    public string Vpp { get; init; } = null!;
    public string? Str { get; init; }
    public string File { get; init; } = null!;
    public string? StrName { get; init; }
    public string FileName { get; init; } = null!;
    public string FileExt { get; init; } = null!;
    public long DataOffset { get; init; }
    public long FileIndex { get; init; }
    public long FileOffset { get; init; }
    public long FileSize { get; init; }
}

[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
public class AsmEntry
{
    /// <summary>
    /// Autoincrement index purely for SQL reasons
    /// </summary>
    public string Id { get; init; } = null!;
    public string Vpp { get; init; } = null!;
    public string AsmName { get; init; } = null!;
    public string Container { get; init; } = null!;
    public string ContainerName { get; init; } = null!;
    public string ContainerExt { get; init; } = null!;
    public string Primitive { get; init; } = null!;
    public string PrimitiveName { get; init; } = null!;
    public string PrimitiveExt { get; init; } = null!;
    public long DataOffset { get; init; }
    public long CompressedDataLength { get; init; }
    public long SplitExtIndex { get; init; }
    public long CpuSize { get; init; }
    public bool CpuSizePresent { get; init; }
    public long GpuSize { get; init; }
    public bool GpuSizePresent { get; init; }
    public string ContainerFlags { get; set; } = null!;
    public string ContainerType { get; set; } = null!;
    public string Allocator { get; set; } = null!;
    public string PrimitiveFlags { get; set; } = null!;
    public string PrimitiveType { get; set; } = null!;
    public bool IsVirtual { get; set; }
}
