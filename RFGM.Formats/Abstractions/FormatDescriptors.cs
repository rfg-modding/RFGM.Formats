using System.Collections.Immutable;
using RFGM.Formats.Abstractions.Descriptors;

namespace RFGM.Formats.Abstractions;

public static class FormatDescriptors
{
    public static IFormatDescriptor MatchForDecoding(string name) => AllDescriptors.FirstOrDefault(x => x.DecodeMatch(name)) ?? RegularFile;

    public static IFormatDescriptor MatchForEncoding(string name) => AllDescriptors.FirstOrDefault(x => x.EncodeMatch(name)) ?? RegularFile;

    private static readonly IReadOnlyList<IFormatDescriptor> Instances = new List<IFormatDescriptor>
    {
        new Str2Descriptor(),
        new VppDescriptor(),
        new PegDescriptor(),
        new XmlDescriptor(),
        new TextureDescriptor(),
        new LocatextDescriptor()
    };

    private static readonly ImmutableHashSet<IFormatDescriptor> AllDescriptors = Instances.ToHashSet().ToImmutableHashSet();

    public static readonly IReadOnlyList<string> UnpackExt = Instances.Where(x => x.IsContainer).Where(x => x.CanDecode).SelectMany(x => x.CanDecodeExtensions).Order().ToList();

    public static readonly IReadOnlyList<string> PackExt = Instances.Where(x => x.IsContainer).Where(x => x.CanEncode).SelectMany(x => x.CanEncodeExtensions).Order().ToList();

    public static readonly IReadOnlyList<string> DecodeExt = Instances.Where(x => !x.IsContainer).Where(x => x.CanDecode).SelectMany(x => x.CanDecodeExtensions).Order().ToList();

    public static readonly IReadOnlyList<string> EncodeExt = Instances.Where(x => !x.IsContainer).Where(x => x.CanEncode).SelectMany(x => x.CanEncodeExtensions).Order().ToList();

    public static readonly IFormatDescriptor RegularFile = new RegularFileDescriptor();
}
