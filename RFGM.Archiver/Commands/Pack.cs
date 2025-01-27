using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using RFGM.Archiver.Models;
using RFGM.Archiver.Models.Messages;
using RFGM.Formats;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Peg;

namespace RFGM.Archiver.Commands;

public class Pack : Command
{
    private readonly Argument<List<string>> inputArg = new("input", "Directory to pack. Can specify multiple directories");

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
    * do not pack formatted xml-like files";

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
        if (!input.Any())
        {
            return await context.ForcePrintHelp(this);
        }

        var output = context.ParseResult.GetValueForOption(outputArg)!;
        var isDefault =context.ParseResult.GetValueForOption(defaultArg);
        var metadata = context.ParseResult.GetValueForOption(metadataArg);
        var force = context.ParseResult.GetValueForOption(forceArg);
        var parallel = context.ParseResult.GetValueForOption(parallelArg);
        var settings = isDefault
            ? PackSettings.Default
            : new PackSettings(metadata, force);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        await archiver.Pack(input, output, parallel, settings, token);
        return 0;
    }
}
