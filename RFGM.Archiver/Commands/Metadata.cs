using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using RFGM.Archiver.Models;
using RFGM.Formats;

namespace RFGM.Archiver.Commands;

public class Metadata : Command
{
    private readonly Argument<List<string>> inputArg = new("input", "File to parse. Can specify multiple files or directories");

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

    public override string? Description => $@"Parse files, write information to {Constants.MetadataFile} for analysis";

    public Metadata() : base(nameof(Metadata).ToLowerInvariant())
    {
        AddArgument(inputArg);
        AddOption(optimizeArg);
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

        var parallel = context.ParseResult.GetValueForOption(parallelArg);
        var optimizeFor = context.ParseResult.GetValueForOption(optimizeArg);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        var settings = UnpackSettings.Meta with { OptimizeFor = optimizeFor };
        await archiver.UnpackMetadata(input, parallel, settings, token);
        return 0;
    }
}
