using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;

namespace RFGM.Archiver.Args;

public class AppRootCommand : RootCommand
{
    public override string? Description => """


                          ___ ___ ___ __  __                            
                         | _ \ __/ __|  \/  |             
                         |   / _| (_ | |\/| |             
                         |_|_\_| \___|_|  |_|             
                  /_\  _ _ __| |_ (_)_ _____ _ _ 
                 / _ \| '_/ _| ' \| \ V / -_) '_|
                /_/ \_\_| \__|_||_|_|\_/\___|_|               
            
            
          🔨 Packer-Unpacker for RFG Re-MARS-tered 🔨

 ╔═════════════════════════════════════════════════════════════╗
 ║ BASIC USAGE: drag&drop files on the .exe to unpack and pack ║
 ║                                                             ║
 ╟─ Pack Examples ─────────────────────────────────────────────╢
 ║ items.vpp_pc + archiver.exe = .unpack/items.vpp_pc/...      ║
 ║ test.cpeg_pc + archiver.exe = .unpack/test.cpeg_pc/*.png    ║
 ╟─ Unpack Examples ───────────────────────────────────────────╢
 ║ modfolder.vpp_pc/ + archiver.exe = .pack/modfolder.vpp_pc   ║
 ║ textures.cpeg_pc/ + archiver.exe = .pack/textures.cpeg_pc   ║
 ╚═════════════════════════════════════════════════════════════╝

Advanced: use commandline to see --help and specify options
""";

    private readonly Argument<List<string>> inputArg = new("input", "any supported archive to unpack, or a folder to pack")
    {
        IsHidden = true
    };

    public AppRootCommand()
    {
        AddArgument(inputArg);
        //AddCommand(new DragDrop());
        AddCommand(new Unpack());
        //AddCommand(new Pack());

        Handler = CommandHandler.Create(Handle);
    }

    /// <summary>
    /// Simplified scenario where user drops files on the .exe
    /// </summary>
    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        var input = context.ParseResult.GetValueForArgument(inputArg);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        await archiver.ProcessInput(input, token);
        return 0;
    }

}
