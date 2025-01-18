using System.Collections.Concurrent;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Filters;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;
using RFGM.Archiver;
using RFGM.Archiver.Args;
using RFGM.Archiver.Models;
using RFGM.Archiver.Services;
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
                services.AddTransient<ImageConverter>();
                services.AddSingleton<Worker>();
                services.AddSingleton<SupportedFormats>();
                services.AddSingleton(SetupRecyclableMemoryStream);
                services.AddLogging(SetupLogs);
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
    .UseHelp(ctx => ctx.HelpBuilder.CustomizeLayout(HackHelpLayout))
    .AddMiddleware(async (context, next) =>
    {
        var log = context.GetHost().Services.GetRequiredService<ILogger<Program>>();
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed!");
        }
    })
    .UseDefaults()
    .Build();
//args = new[] { "unpeg", @"c:\vault\rfg\unpack_all" };
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


void SetupLogs(ILoggingBuilder x)
{
    x.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);

    var config = new LoggingConfiguration();

    var layout = Layout.FromString("${date:format=HH\\:mm\\:ss} ${pad:padding=5:inner=${level:uppercase=true}} ${message}${onexception:${newline}${exception}}");
    var console = new ConsoleTarget("console");
    console.Layout = layout;
    var rule1 = new LoggingRule("*", NLog.LogLevel.Info, NLog.LogLevel.Off, new AsyncTargetWrapper(console, 10000, AsyncTargetWrapperOverflowAction.Discard));

    var file = new FileTarget("file");
    file.FileName = ".rfgm.archiver.log";
    file.Layout = layout;
    var rule2 = new LoggingRule("*", NLog.LogLevel.Trace, NLog.LogLevel.Off, new AsyncTargetWrapper(file, 10000, AsyncTargetWrapperOverflowAction.Grow));

    var filterRule = new LoggingRule("*", NLog.LogLevel.Trace, NLog.LogLevel.Off, new NullTarget());
    filterRule.Filters.Add(new ConditionBasedFilter {Action = FilterResult.IgnoreFinal, Condition = "'${logger}' == 'Microsoft.Hosting.Lifetime'"});
    filterRule.Filters.Add(new ConditionBasedFilter {Action = FilterResult.IgnoreFinal, Condition = "'${logger}' == 'Microsoft.Extensions.Hosting.Internal.Host'"});
    /*filterRule.Filters.Add(new ConditionBasedFilter
    {
        Action = FilterResult.IgnoreFinal,
        Condition = "'${logger}' == 'SyncFaction.Packer.VppArchiver'"
    });*/

    config.AddRule(filterRule);
    config.AddRule(rule1);
    config.AddRule(rule2);

    x.AddNLog(config);
}

RecyclableMemoryStreamManager SetupRecyclableMemoryStream(IServiceProvider sp)
{
    var log = sp.GetRequiredService<ILogger<RecyclableMemoryStreamManager>>();
    var manager = new RecyclableMemoryStreamManager();
    var tags = new ConcurrentDictionary<string, byte>();
    manager.StreamCreated += (_, eventArgs) =>
    {
        log.LogDebug("Stream created: {tag}", eventArgs.Tag);

        if (!tags.TryAdd(eventArgs.Tag!, 1))
        {
            throw new InvalidOperationException($"Duplicate stream tag [{eventArgs.Tag}]");
        }
    };
    manager.StreamDisposed += (_, eventArgs) =>
    {
        log.LogDebug("Stream disposed: {tag}", eventArgs.Tag);
        if (!tags.TryRemove(eventArgs.Tag!, out var _))
        {
            throw new InvalidOperationException($"Missing stream tag [{eventArgs.Tag}]");
        }
    };
    manager.UsageReport += (_, eventArgs) =>
    {
        //var info = JsonSerializer.Serialize(eventArgs);
        log.LogDebug("Streams: {n} in use", tags.Count);
    };
    return manager;
}

IEnumerable<HelpSectionDelegate> HackHelpLayout(HelpContext _)
{
    var layout = HelpBuilder.Default.GetLayout();
    if (runStandalone)
    {
        return layout
            .Take(0) // Skip all output
            .Append(context => Console.WriteLine(context.Command.Description)); // Display banner
    }

    return layout;
}
