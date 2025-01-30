using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using RFGM.Archiver.Models;

namespace RFGM.Archiver.Commandline;

public class Pack : Command
{
    private readonly Argument<List<string>> inputArg = new("input", "Directory to pack. Can specify multiple directories"){
        Arity = ArgumentArity.OneOrMore
    };

    private readonly Option<string?> outputArg = new([
            "-o",
            "--output",
        ],
        () => Constants.DefaultPackDir,
        $"Output path, can be relative to input");

    private readonly Option<bool> defaultArg = new([
            "-d",
            "--default"
        ],
        $"Use default pack settings [ (no flags) ]. Other flags will be ignored!");

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

    public override string? Description => @"Pack directories into containers
NOTES:
    * not recursive, nested directories are ignored
    * directory to pack must have valid name with required container properties
    * either specify order prefix for every file or rely on sorting by name
    * keep original formatting in xml-like files, game crashes if they are reformatted
    * use -m to verify packed entries, compare with metadata obtained during unpack";

    public Pack() : base(nameof(Pack).ToLowerInvariant())
    {
        AddArgument(inputArg);

        AddOption(defaultArg);
        AddOption(forceArg);
        AddOption(metadataArg);
        AddOption(outputArg);
        AddOption(parallelArg);
        Handler = CommandHandler.Create(Handle);
    }

    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        var input = context.ParseResult.GetValueForArgument(inputArg);
        var output = context.ParseResult.GetValueForOption(outputArg)!;
        var isDefault =context.ParseResult.GetValueForOption(defaultArg);
        var metadata = context.ParseResult.GetValueForOption(metadataArg);
        var force = context.ParseResult.GetValueForOption(forceArg);
        var parallel = context.ParseResult.GetValueForOption(parallelArg);
        var settings = isDefault
            ? PackSettings.Default
            : new PackSettings(metadata, force);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        return (int) await archiver.CommandPack(input, output, parallel, settings, token);
    }
}
