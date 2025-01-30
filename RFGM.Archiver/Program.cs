using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using RFGM.Archiver.Commandline;
using RFGM.Archiver.Services;
using RFGM.Archiver.Services.Handlers;
using RFGM.Formats.Localization;
using RFGM.Formats.Peg;
using RFGM.Formats.Vpp;

namespace RFGM.Archiver;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var runStandalone = !string.IsNullOrEmpty(Process.GetCurrentProcess().MainWindowTitle);
        return (int)await RunMain(args, runStandalone, LogLevel.Debug);
    }

    public static async Task<ExitCode> RunMain(string[] args, bool runStandalone, LogLevel consoleLogLevel, Action<HostBuilderContext, IServiceCollection>? configureTestServices=null)
    {
        var root = new AppRootCommand();
        var runner = new CommandLineBuilder(root)
            .UseHost(_ => new HostBuilder(), builder => builder
                .UseConsoleLifetime()
                .ConfigureServices(services => services.AddLogging(loggingBuilder => ArchiverUtils.SetupLogs(loggingBuilder, consoleLogLevel)))
                .ConfigureServices(ConfigureServices)
                .ConfigureServices(configureTestServices ?? ((_, _) => { }))
            )
            .UseHelp(ctx => ctx.HelpBuilder.CustomizeLayout(_ => ArchiverUtils.HackHelpLayout(runStandalone)))
            .AddMiddleware(ArchiverUtils.LogExceptionMiddleware)
            .UseDefaults()
            .Build();
        if (!args.Any())
        {
            args = ["--help"]; // hack to invoke help with no args and have default command at same time
        }
        var result = (ExitCode)await runner.InvokeAsync(args);
        if (runStandalone)
        {
            // user ran .exe directly from explorer, show help and don't close
            Console.WriteLine("Press enter to close");
            Console.ReadLine();
        }

        return result;
    }

    static void ConfigureServices(HostBuilderContext _, IServiceCollection services)
    {
        services.AddTransient<IVppArchiver, VppArchiver>();
        services.AddTransient<IPegArchiver, PegArchiver>();

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<Services.Archiver>();
        services.AddSingleton<ArchiverState>();
        services.AddSingleton<MetadataWriter>();
        services.AddSingleton<ImageConverter>();
        services.AddSingleton<Worker>();
        services.AddSingleton<Rick>();
        services.AddSingleton<FileManager>();
        services.AddSingleton<LocatextReader>();

        services.Scan(selector =>
        {
            selector.FromAssemblyOf<Program>()
                .AddClasses(filter => filter.AssignableTo(typeof(IHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime();
        });
    }
}

/*
TODO:
* ImageConverter: colors are off when texture is converted to PNG, especially in normal maps. figure out why, maybe just wrong conversion in dxtex/imgsharp
*/
