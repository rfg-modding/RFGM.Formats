using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using RFGM.Archiver.Models;
using RFGM.Formats;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Commandline;

public class Unpack : Command
{
    private readonly Argument<List<string>> inputArg = new("input", "File to unpack. Can specify multiple files or directories")
    {
        Arity = ArgumentArity.OneOrMore
    };

    private readonly Option<string?> outputArg = new([
            "-o",
            "--output",
        ],
        () => Constants.DefaultUnpackDir,
        $"Output path, can be relative to input");

    private readonly Option<bool> defaultArg = new([
            "-d",
            "--default"
        ],
        $"Use default unpack settings [ -r -l ]. Other flags will be ignored!");

    private readonly Option<string> filterArg = new([
            "--filter",
        ],
        () => "**/*",
        "File mask to extract, including path in nested containers. Example: **/*.tga, **/*.str2_pc/metal*")
    {
        ArgumentHelpName = "glob"
    };

    private readonly Option<bool> xmlArg = new([
            "-x",
            "--xmlFormat"
        ],
        "Format xml-like files (.xtbl .dtdox .gtdox) for readability. Some formatted files will crash game if packed!");

    private readonly Option<bool> localizationArg = new([
            "-l",
            "--localization"
        ],
        "Decode localization files (.rfglocatext) into editable xml");

    private readonly Option<bool> recursiveArg = new([
            "-r",
            "--recursive"
        ],
        $"Unpack nested containers recursively in {Constants.DefaultUnpackDir} directories");

    private readonly Option<bool> noPropsArg = new([
            "-n",
            "--noProperties"
        ],
        "Unpack entries without important properties encoded in filename. Missing data makes these files impossible to pack!");


    private readonly Option<ImageFormat> textureArg = new([
            "-t",
            "--textureFormat"
        ],
        () => ImageFormat.png,
        "Convert textures to specified format"
        );

    private readonly Option<bool> metadataArg = new([
            "-m",
            "--metadata"
        ],
        $"Write {Constants.MetadataFile} file for analysis: entries, sizes, hashes, etc. Adds CPU load to compute hashes!");

    private readonly Option<bool> forceArg = new([
            "-f",
            "--force"
        ],
        "Overwrite output if exists");

    private readonly Option<int> parallelArg = new([
            "-p",
            "--parallel"
        ],
        () => Environment.ProcessorCount,
        "Number of parallel tasks. Defaults to processor core count, use 1 for lower RAM usage")
    {
        ArgumentHelpName = "N"
    };

    private readonly Option<OptimizeFor> optimizeArg = new([
            "--optimizeFor"
        ],
        () => OptimizeFor.speed,
        "Optimization profile for reading certain formats");


    private readonly Option<bool> skipArg = new([
            "-s",
            "--skipContainers"
        ],
        "Do not write container files when unpacking, with -r will unpack all regular files without wasting space");

    public override string? Description => @"Extract files and containers
NOTES:
    * it is generally safe to pack data which was unpacked with default settings
    * preserve file and directory names, they contain properties required to pack them
    * added file names must follow same format as unpacked files";

    public Unpack() : base(nameof(Unpack).ToLowerInvariant())
    {
        AddArgument(inputArg);
        //AddArgument(outputArg);

        AddOption(defaultArg);
        AddOption(filterArg);
        AddOption(forceArg);
        AddOption(localizationArg);
        AddOption(metadataArg);
        AddOption(noPropsArg);
        AddOption(optimizeArg);
        AddOption(outputArg);
        AddOption(parallelArg);
        AddOption(recursiveArg);
        AddOption(skipArg);
        AddOption(textureArg);
        AddOption(xmlArg);
        Handler = CommandHandler.Create(Handle);
    }

    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        var input = context.ParseResult.GetValueForArgument(inputArg);
        var output = context.ParseResult.GetValueForOption(outputArg)!;
        var isDefault =context.ParseResult.GetValueForOption(defaultArg);
        var filter = context.ParseResult.GetValueForOption(filterArg)!;
        var xmlFormat = context.ParseResult.GetValueForOption(xmlArg);
        var recursive = context.ParseResult.GetValueForOption(recursiveArg);
        var noProps = context.ParseResult.GetValueForOption(noPropsArg);
        var texture = context.ParseResult.GetValueForOption(textureArg);
        var metadata = context.ParseResult.GetValueForOption(metadataArg);
        var force = context.ParseResult.GetValueForOption(forceArg);
        var parallel = context.ParseResult.GetValueForOption(parallelArg);
        var optimize = context.ParseResult.GetValueForOption(optimizeArg);
        var skip = context.ParseResult.GetValueForOption(skipArg);
        var localization = context.ParseResult.GetValueForOption(localizationArg);
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase).AddInclude(filter);
        var settings = isDefault
            ? UnpackSettings.Default
            : new UnpackSettings(matcher, texture, optimize, skip, !noProps, xmlFormat, recursive, metadata, force, localization);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        return (int) await archiver.CommandUnpack(input, output, parallel, settings, token);
    }
}
