using System.Collections.Immutable;
using System.IO.Abstractions;

namespace RFGM.Formats.Abstractions;

public static class FormatDescriptors
{
    public static IFormatDescriptor DetermineByName(string name) => AllDescriptors.FirstOrDefault(x => x.Match(name)) ?? RegularFile;

    public static IFormatDescriptor DetermineByFileSystem(IFileSystemInfo info) => AllDescriptors.FirstOrDefault(x => x.Match(info)) ?? RegularFile;

    private static readonly IReadOnlyList<IFormatDescriptor> Instances = new List<IFormatDescriptor>
    {
        new Str2Descriptor(),
        new VppDescriptor(),
        new PegDescriptor(),
        new XmlDescriptor(),
        new RawTextureDescriptor(),
        new TextureDescriptor(),
    };

    private static readonly ImmutableHashSet<IFormatDescriptor> AllDescriptors = Instances.ToHashSet().ToImmutableHashSet();

    public static readonly IReadOnlyList<string> UnpackExt = Instances.Where(x => x.IsContainer).Where(x => x.CanDecode).SelectMany(x => x.Extensions).Order().ToList();

    public static readonly IReadOnlyList<string> PackExt = Instances.Where(x => x.IsContainer).Where(x => x.CanEncode).SelectMany(x => x.Extensions).Order().ToList();

    public static readonly IReadOnlyList<string> DecodeExt = Instances.Where(x => !x.IsContainer).Where(x => x.CanDecode).SelectMany(x => x.Extensions).Order().ToList();

    public static readonly IReadOnlyList<string> EncodeExt = Instances.Where(x => !x.IsContainer).Where(x => x.CanEncode).SelectMany(x => x.Extensions).Order().ToList();

    public static readonly IFormatDescriptor RegularFile = new RegularFileDescriptor();
}
