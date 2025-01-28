using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Commands;

public class AppRootCommand : RootCommand
{
    /// <summary>
    /// Should fit into default console window when .exe is double-clicked
    /// </summary>
    public override string? Description => $"""
                            ___ ___ ___ __  __                            
                           | _ \ __/ __|  \/  |             
                           |   / _| (_ | |\/| |             
                           |_|_\_| \___|_|  |_|             
                    /_\  _ _ __| |_ (_)_ _____ _ _ 
                   / _ \| '_/ _| ' \| \ V / -_) '_|
                  /_/ \_\_| \__|_||_|_|\_/\___|_|               
              
            🔨 Packer-Unpacker for RFG Re-MARS-tered 🔨         

 ╔════════════════╤══════════════════════════════════════════════╗
 ║ BASIC USAGE    ╪ drag&drop files on the .exe to unpack/pack   ║
 ║ advanced       ╪ use commandline for more features            ║
 ║ help/bugreport ╪ https://discord.gg/factionfiles #rfg-modding ║
 ╟────────────────┴─────── Pack Examples ────────────────────────╢
 ║ items.vpp_pc + archiver.exe = .unpack/items.vpp_pc/...        ║
 ║ test.cpeg_pc + archiver.exe = .unpack/test.cpeg_pc/*.png      ║
 ╟─────────────────────── Unpack Examples ───────────────────────╢
 ║ modfolder.vpp_pc/ + archiver.exe = .pack/modfolder.vpp_pc     ║
 ║ textures.cpeg_pc/ + archiver.exe = .pack/textures.cpeg_pc     ║
 ╚═══════════════════════════════════════════════════════════════╝

Supported formats
Unpack: {string.Join(" ", FormatDescriptors.UnpackExt)}
Pack:   {string.Join(" ", FormatDescriptors.PackExt)}
Decode: {string.Join(" ", FormatDescriptors.DecodeExt)}
Encode: {string.Join(" ", FormatDescriptors.EncodeExt)}
""";

    private readonly Argument<List<string>> inputArg = new("input", "Any supported file to unpack, or a directory to pack")
    {
        IsHidden = true,
        Arity = ArgumentArity.OneOrMore
    };

    public AppRootCommand()
    {
        AddArgument(inputArg);
        AddCommand(new Unpack());
        AddCommand(new Metadata());
        AddCommand(new Test());
        AddCommand(new Pack());

        Handler = CommandHandler.Create(Handle);
    }

    /// <summary>
    /// Simplified scenario where user drops files on the .exe
    /// </summary>
    private async Task<int> Handle(InvocationContext context, CancellationToken token)
    {
        var input = context.ParseResult.GetValueForArgument(inputArg);
        var archiver = context.GetHost().Services.GetRequiredService<Services.Archiver>();
        await archiver.ProcessDefault(input, token);
        return 0;
    }

}
