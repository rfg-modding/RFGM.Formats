using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RFGM.Archiver.Services;

public class Rick(ILogger<Rick> log)
{
    public virtual void Roll()
    {
        log.LogWarning("U WOT M8?");
        using var proc = new Process();
        proc.StartInfo.UseShellExecute = true;
        proc.StartInfo.FileName = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly93d3cueW91dHViZS5jb20vd2F0Y2g/dj1kUXc0dzlXZ1hjUQ=="));
        proc.Start();
    }
}
