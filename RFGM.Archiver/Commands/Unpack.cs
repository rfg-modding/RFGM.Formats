using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Archiver.Services.Handlers;
using RFGM.Formats;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Commands;

public class Unpack : Command
{
    private readonly Argument<List<string>> inputArg = new("input", "File to unpack. Can specify multiple files or directories");

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
        $"Use default unpack settings [ -i -r ]. Other flags will be ignored!");

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

    private readonly Option<bool> recursiveArg = new([
            "-r",
            "--recursive"
        ],
        $"Unpack nested containers recursively in {Constants.DefaultUnpackDir} directories");

    private readonly Option<bool> indexArg = new([
            "-i",
            "--index"
        ],
        "Unpack entries with ordered index filename prefix like \"00001 weapons.xtbl\". Used for packing");


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
        AddOption(indexArg);
        AddOption(metadataArg);
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
        if (!input.Any())
        {
            return await context.ForcePrintHelp(this);
        }

        var output = context.ParseResult.GetValueForOption(outputArg)!;
        var isDefault =context.ParseResult.GetValueForOption(defaultArg);
        var filter = context.ParseResult.GetValueForOption(filterArg)!;
        var xmlFormat = context.ParseResult.GetValueForOption(xmlArg);
        var recursive = context.ParseResult.GetValueForOption(recursiveArg);
        var index = context.ParseResult.GetValueForOption(indexArg);
        var texture = context.ParseResult.GetValueForOption(textureArg);
        var metadata = context.ParseResult.GetValueForOption(metadataArg);
        var force = context.ParseResult.GetValueForOption(forceArg);
        var parallel = context.ParseResult.GetValueForOption(parallelArg);
        var optimize = context.ParseResult.GetValueForOption(optimizeArg);
        var skip = context.ParseResult.GetValueForOption(skipArg);
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase).AddInclude(filter);
        var settings = isDefault
            ? UnpackSettings.Default
            : new UnpackSettings(matcher, texture, optimize, skip, index, xmlFormat, recursive, metadata, force);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        await archiver.Unpack(input, output, parallel, settings, token);
        return 0;
    }
}
