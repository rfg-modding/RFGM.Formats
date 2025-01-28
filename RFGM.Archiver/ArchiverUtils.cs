using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Filters;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;


namespace RFGM.Archiver;

public static class ArchiverUtils
{
    public static IEnumerable<HelpSectionDelegate> HackHelpLayout(bool runStandalone)
    {
        var layout = HelpBuilder.Default.GetLayout();
        if (runStandalone)
        {
            return layout
                .Take(0) // Skip all output
                .Append(context => Console.Write(context.Command.Description)); // Display banner
        }

        return layout;
    }

    public static void SetupLogs(ILoggingBuilder x)
    {
        x.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);

        var config = new LoggingConfiguration();

        var noColor = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
        TargetWithLayoutHeaderAndFooter console = noColor
            ? new ConsoleTarget("console")
            : new ColoredConsoleTarget("console")
            {
                RowHighlightingRules =
                {
                    new ConsoleRowHighlightingRule
                    {
                        Condition = "level == LogLevel.Trace",
                        ForegroundColor = ConsoleOutputColor.DarkGray
                    },
                    new ConsoleRowHighlightingRule
                    {
                        Condition = "level == LogLevel.Debug",
                        ForegroundColor = ConsoleOutputColor.Gray
                    },
                    new ConsoleRowHighlightingRule
                    {
                        Condition = "level == LogLevel.Info",
                        ForegroundColor = ConsoleOutputColor.Green
                    },
                    new ConsoleRowHighlightingRule
                    {
                        Condition = "level == LogLevel.Warn",
                        ForegroundColor = ConsoleOutputColor.Yellow
                    },
                    new ConsoleRowHighlightingRule
                    {
                        Condition = "level == LogLevel.Error",
                        ForegroundColor = ConsoleOutputColor.Magenta
                    },
                    new ConsoleRowHighlightingRule
                    {
                        Condition = "level == LogLevel.Fatal",
                        ForegroundColor = ConsoleOutputColor.Red
                    },
                },
            };
        console.Layout = Layout.FromString("${date:format=HH\\:mm\\:ss} ${message}${onexception:${newline}${exception}}");
        var rule1 = new LoggingRule("*", NLog.LogLevel.Debug, NLog.LogLevel.Off, new AsyncTargetWrapper(console, 10000, AsyncTargetWrapperOverflowAction.Discard));

        var file = new FileTarget("file");
        file.FileName = ".rfgm.archiver.log";
        file.DeleteOldFileOnStartup = true;
        file.Layout = Layout.FromString("${date:format=HH\\:mm\\:ss} ${callsite} ${pad:padding=5:inner=${level:uppercase=true}} ${message}${onexception:${newline}${exception}}");
        var rule2 = new LoggingRule("*", NLog.LogLevel.Trace, NLog.LogLevel.Off, new AsyncTargetWrapper(file, 10000, AsyncTargetWrapperOverflowAction.Grow));

        var filterRule = new LoggingRule("*", NLog.LogLevel.Trace, NLog.LogLevel.Off, new NullTarget());
        filterRule.Filters.Add(new ConditionBasedFilter {Action = FilterResult.IgnoreFinal, Condition = "'${logger}' == 'Microsoft.Hosting.Lifetime'"});
        filterRule.Filters.Add(new ConditionBasedFilter {Action = FilterResult.IgnoreFinal, Condition = "'${logger}' == 'Microsoft.Extensions.Hosting.Internal.Host'"});
        config.AddRule(filterRule);
        config.AddRule(rule1);
        config.AddRule(rule2);

        x.AddNLog(config);
    }

    public static async Task LogExceptionMiddleware(InvocationContext context, Func<InvocationContext, Task> next)
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
    }

}
