using Microsoft.Extensions.Logging;

namespace RFGM.Archiver.Services;

public class ProgressLogger
{
    private int current;
    private double lastReported;
    private DateTime lastReportedAt;

    private readonly string message;
    private readonly int total;
    private readonly ILogger log;
    private readonly double reportStep;
    private readonly TimeSpan reportTime;


    public ProgressLogger(string message, int total, ILogger log, double reportStep=0.1, TimeSpan? reportTime=null)
    {
        this.message = message;
        this.total = total;
        this.log = log;
        this.reportStep = reportStep;
        this.reportTime = reportTime ?? TimeSpan.FromSeconds(5);

        log.LogInformation("{message}: {progress:0%}", message, 0);
        this.lastReportedAt = DateTime.UtcNow;
    }

    public void Tick()
    {
        current++;
        var newProgress = (double)current / total;
        var now = DateTime.UtcNow;
        var reportByProgress = newProgress - lastReported >= reportStep;
        var reportByTime = now - lastReportedAt >= reportTime;
        var reportByCompletion = current == total;
        if ( reportByCompletion || (reportByProgress && reportByTime) )
        {
            log.LogInformation("{message}: {progress:0%}", message, newProgress);
            lastReported = newProgress;
            lastReportedAt = now;
        }
    }
}