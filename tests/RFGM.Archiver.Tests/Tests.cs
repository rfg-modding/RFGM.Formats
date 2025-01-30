using RFGM.Formats.Abstractions;

namespace RFGM.Archiver.Tests;

public class Tests
{
    [Test]
    public void NameFormatTest()
    {
        List<string> names = [
            "ab",
            "a.b",
            "x ab",
            "x a.b",
            "xy ab",
            "xy a.b",
            "x.y ab",
            "x.y a.b",
        ];

        List<string> props = [
            "",
            "{c}"
        ];

        List<string> exts = [
            "",
            ".d"
        ];

        foreach (var n in names)
        {
            foreach (var p in props)
            {
                foreach (var e in exts)
                {
                    var str = $"{n}{p}{e}";
                    var (name, prop, ext) = RunRegex(str);
                    var ok = n == name && p == prop && e == ext ? "OK" : "  ";
                    Console.WriteLine($"[{str,20}] {ok} = [{name,8}] [{prop,8}] [{ext,8}]");
                }
            }
        }
        Assert.Pass();
    }

    (string name, string prop, string ext) RunRegex(string s)
    {
        var m = FormatDescriptorBase.PropertyNameFormat.Match(s);
        var ext = m.Groups["ext"].Value;
        var name = m.Groups["nameNoExt"].Value;
        var prop = m.Groups["props"].Value;
        return (name, prop, ext);
    }
}
