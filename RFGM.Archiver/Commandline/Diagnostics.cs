using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;

namespace RFGM.Archiver.Commandline;

public class Diagnostics : Command
{
    private readonly Argument<List<string>> inputArg = new("input", "test input"){
        Arity = ArgumentArity.ZeroOrMore
    };

    private readonly Option<int> parallelArg = new([
            "-p",
            "--parallel"
        ],
        () => Environment.ProcessorCount,
        "number of parallel tasks. Defaults to processor core count. Use 1 for lower RAM usage")
    {
        ArgumentHelpName = "N"
    };

    public override string? Description => @"Run test logic for debugging";

    public Diagnostics() : base(nameof(Diagnostics).ToLowerInvariant())
    {
        IsHidden = true;
        AddArgument(inputArg);
        AddOption(parallelArg);
        Handler = CommandHandler.Create(Handle);
    }

    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        var input = context.ParseResult.GetValueForArgument(inputArg);
        var parallel = context.ParseResult.GetValueForOption(parallelArg);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        return (int) await archiver.CommandDiagnostics(input, parallel, token);
    }
}
