using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;

namespace RFGM.Archiver.Commandline;

public class AppRootCommand : RootCommand
{
    public override string Description => ArchiverUtils.Banner;
    private readonly Argument<List<string>> inputArg = new("input", "Any supported file to unpack, or a directory to pack")
    {
        IsHidden = true,
        Arity = ArgumentArity.OneOrMore
    };

    private readonly Option<bool> forceArg = new([
            "-f",
            "--force"
        ],
        "Overwrite output if exists")
    {
        IsHidden = true
    };

    public AppRootCommand()
    {
        AddArgument(inputArg);
        AddOption(forceArg);
        AddCommand(new Unpack());
        AddCommand(new Metadata());
        AddCommand(new AppTest());
        AddCommand(new Pack());

        Handler = CommandHandler.Create(Handle);
    }

    /// <summary>
    /// Simplified scenario where user drops files on the .exe
    /// </summary>
    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        var input = context.ParseResult.GetValueForArgument(inputArg);
        var force = context.ParseResult.GetValueForOption(forceArg);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        return (int)await archiver.CommandDefault(input, force, token);
    }

}
