using System.Collections.Immutable;
using System.IO.Abstractions;

namespace RFGM.Archiver.Models;

public class SupportedFormats(IFileSystem fileSystem)
{
    public static readonly ImmutableHashSet<FileFormat> CanRead = new HashSet<FileFormat>
    {
        FileFormat.Vpp,
        FileFormat.Str2,
        FileFormat.Peg
    }.ToImmutableHashSet();

    public static readonly ImmutableHashSet<FileFormat> CanWriteFully = new HashSet<FileFormat>
    {
        FileFormat.Vpp,
        FileFormat.Peg
    }.ToImmutableHashSet();

    public static readonly ImmutableHashSet<FileFormat> CanWriteSomehow = new HashSet<FileFormat>
    {
        FileFormat.Vpp,
        FileFormat.Str2,
        FileFormat.Peg
    }.ToImmutableHashSet();

    public FileFormat GuessByExtension(string extension)
    {
        return extension switch
        {
            ".vpp_pc" => FileFormat.Vpp,
            ".str2_pc" => FileFormat.Str2,
            ".cpeg_pc" or ".gpeg_pc" or ".cvbm_pc" or ".gvbm_pc" => FileFormat.Peg,
            _ => FileFormat.Unsupported
        };
    }
}
