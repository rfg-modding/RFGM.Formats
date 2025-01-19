using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using RFGM.Formats;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Args;

public class Unpack : Command
{
    private readonly Argument<string> archiveArg = new("archive", "any supported archive to unpack, or a folder, globs allowed");
    private readonly Argument<string> filesArg = new("filter", () => "*", "files inside archive to extract, globs allowed. lookup is not recursive!");
    private readonly Argument<string> outputArg = new("output", () => Constants.DefaultDir, "output path");

    private readonly Option<bool> xmlFormat = new([
            "-x",
            "--xmlFormat"
        ],
        "format xml-like files (.xtbl .dtdox .gtdox) for readability, some files will become unusable in game");

    private readonly Option<bool> recursive = new([
            "-r",
            "--recursive"
        ],
        $"unpack nested archives (typically .str2_pc) recursively in {Constants.DefaultDir} subfolder");

    private readonly Option<List<ImageFormat>> textures = new([
            "-t",
            "--textures"
        ],
        $"unpack textures from containers (.cpeg_pc .cvbm_pc .gpeg_pc .gvbm_pc) in {Constants.DefaultDir} subfolder. Specify one or more supported formats: dds png raw")
    {
        ArgumentHelpName = "formats",
        AllowMultipleArgumentsPerToken = true,
    };

    private readonly Option<bool> metadata = new([
            "-m",
            "--metadata"
        ],
        $"write {Constants.MetadataFile} file with archive information: entries, sizes, hashes");

    private readonly Option<bool> force = new([
            "-f",
            "--force"
        ],
        "overwrite output if exists");

    private readonly Option<int> parallel = new([
            "-p",
            "--parallel"
        ],
        "number of parallel tasks. Defaults to processor core count. Use 1 for lower RAM usage")
    {
        ArgumentHelpName = "N"
    };

    private readonly Option<bool> skipArchives = new([
            "-s",
            "--skipArchives"
        ],
        $"do not write nested archive files when unpacking. With -r will unpack all regular files without wasting space");

    public override string? Description => @"Extract archives and containers
Supported formats: " + string.Join(" ", Constants.KnownVppExtensions.Concat(Constants.KnownPegExtensions));

    public Unpack() : base(nameof(Unpack).ToLowerInvariant())
    {

        AddArgument(archiveArg);
        AddArgument(filesArg);
        AddArgument(outputArg);
        AddOption(xmlFormat);
        AddOption(recursive);
        AddOption(skipArchives);
        AddOption(textures);
        AddOption(metadata);
        AddOption(parallel);
        AddOption(force);
        Handler = CommandHandler.Create(Handle);
    }

    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        // TODO configure log level with -v to hide TRACE logs
        // TODO log start and end each operation
        var archive = context.ParseResult.GetValueForArgument(archiveArg);
        var files = context.ParseResult.GetValueForArgument(filesArg);
        var output = context.ParseResult.GetValueForArgument(outputArg);
        var xmlFormat = context.ParseResult.GetValueForOption(this.xmlFormat);
        var recursive = context.ParseResult.GetValueForOption(this.recursive);
        var skipArchives = context.ParseResult.GetValueForOption(this.skipArchives);
        var textures = context.ParseResult.GetValueForOption(this.textures) ?? [];
        var metadata = context.ParseResult.GetValueForOption(this.metadata);
        var force = context.ParseResult.GetValueForOption(this.force);
        var parallel = context.ParseResult.GetValueForOption(this.parallel);
        //var settings = new UnpackSettings(archive, files, output, xmlFormat, recursive, textures, metadata, force, parallel < 1 ? Environment.ProcessorCount : parallel, skipArchives);
        //var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        //await archiver.Unpack(settings, token);
        throw new NotImplementedException();
        return 0;
    }
}
