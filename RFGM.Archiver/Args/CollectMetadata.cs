using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using RFGM.Archiver.Models;
using RFGM.Formats;

namespace RFGM.Archiver.Args;

public class CollectMetadata : Command
{
    private readonly Argument<List<string>> inputArg = new("archive", "any supported archive to unpack, or a folder");

    private readonly Option<bool> hashArg = new([
            "--hash"
        ],
        "compute hashes");

    private readonly Option<int> parallelArg = new([
            "-p",
            "--parallel"
        ],
        () => Environment.ProcessorCount,
        "number of parallel tasks. Defaults to processor core count. Use 1 for lower RAM usage")
    {
        ArgumentHelpName = "N"
    };

    private readonly Option<OptimizeFor> optimizeForArg = new([
            "-o",
            "--optimizeFor"
        ],
        "optimization profile");

    public override string? Description => @"Parse all supported archives and containers, write information to single file for analysis
Supported formats: " + string.Join(" ", Constants.KnownVppExtensions.Concat(Constants.KnownPegExtensions));

    public CollectMetadata() : base(nameof(CollectMetadata).ToLowerInvariant())
    {

        AddArgument(inputArg);
        AddOption(parallelArg);
        AddOption(optimizeForArg);
        AddOption(hashArg);
        Handler = CommandHandler.Create(Handle);
    }

    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        var input = context.ParseResult.GetValueForArgument(inputArg);
        var hash = context.ParseResult.GetValueForOption(this.hashArg);
        var parallel = context.ParseResult.GetValueForOption(this.parallelArg);
        var optimizeFor = context.ParseResult.GetValueForOption(this.optimizeForArg);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        await archiver.CollectMetadata(input, hash, parallel, optimizeFor, token);
        return 0;
    }
}
