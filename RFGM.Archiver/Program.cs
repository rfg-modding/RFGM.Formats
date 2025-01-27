using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Filters;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;
using RFGM.Archiver;
using RFGM.Archiver.Commands;
using RFGM.Archiver.Services;
using RFGM.Archiver.Services.Handlers;
using RFGM.Formats.Localization;
using RFGM.Formats.Peg;
using RFGM.Formats.Vpp;

var runStandalone = !string.IsNullOrEmpty(Process.GetCurrentProcess().MainWindowTitle);
var root = new AppRootCommand();
var runner = new CommandLineBuilder(root).UseHost(_ => new HostBuilder(),
        builder => builder.UseConsoleLifetime()
            .ConfigureServices((_, services) =>
            {
                services.AddTransient<IVppArchiver, VppArchiver>();
                services.AddTransient<IPegArchiver, PegArchiver>();
                services.AddSingleton<IFileSystem, FileSystem>();
                services.AddTransient<Archiver>();
                services.AddSingleton<ImageConverter>();
                services.AddSingleton<Worker>();
                services.AddSingleton<FileManager>();
                services.AddSingleton<LocatextReader>();
                services.AddLogging(ArchiverUtils.SetupLogs);
                services.Scan(selector =>
                {
                    selector.FromAssemblyOf<Program>()
                        .AddClasses(filter => filter.AssignableTo(typeof(IHandler<>)))
                        .AsImplementedInterfaces()
                        .WithScopedLifetime()
                        ;
                });
            })
    )
    .UseHelp(ctx => ctx.HelpBuilder.CustomizeLayout(_ => ArchiverUtils.HackHelpLayout(runStandalone)))
    .AddMiddleware(async (context, next) =>
    {
        var log = context.GetHost().Services.GetRequiredService<ILogger<Program>>();
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            var logPath = Path.Combine(Environment.CurrentDirectory, ".rfgm.archiver.log");
            log.LogTrace(e, "Command failed");
            log.LogCritical("Command failed!\n\t{message}\n\tCheck log for details:\n\t{logPath}", e.Message, logPath);
        }
    })
    .UseDefaults()
    .Build();
if (!args.Any())
{
    // hack to invoke help with no args and have default command at same time
    args = ["--help"];
}
await runner.InvokeAsync(args);

if (runStandalone)
{
    // user ran .exe directly from explorer, show help and don't close
    Console.WriteLine("Press enter to close");
    Console.ReadLine();
}



/*
TODO:
 * ImageConverter: colors are off when texture is converted to PNG, especially in normal maps. figure out why, maybe just wrong conversion in dxtex/imgsharp
 *
 *
 *
    */
