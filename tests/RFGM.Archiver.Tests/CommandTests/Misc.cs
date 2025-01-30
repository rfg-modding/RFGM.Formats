using System.Text.RegularExpressions;

namespace RFGM.Archiver.Tests.CommandTests;

public class Misc
{
    /// <summary>
    /// print existing file paths for use in tests
    /// </summary>
    [Test]
    public void ListFiles()
    {
        var d = new DirectoryInfo("Files");
        var r = new Regex("^.*/Files/(?<path>.*)$");
        var files = d.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(x => x.FullName.Replace('\\', '/'))
            .Select(x => r.Match(x).Groups["path"].Value)
            .ToList();
        var rootOrNestedFiles = files.GroupBy(x => !x.Contains('/')).ToList();
        var nestedFiles = rootOrNestedFiles.First().Order(StringComparer.OrdinalIgnoreCase);
        var rootFiles = rootOrNestedFiles.Last().Order(StringComparer.OrdinalIgnoreCase);
        var result = rootFiles.Concat(nestedFiles);
        Assert.Pass(string.Join("\n", result));
    }

}
