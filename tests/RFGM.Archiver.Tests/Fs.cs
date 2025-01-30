using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;

namespace RFGM.Archiver.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Tests")]
public static class Fs
{
    public static void Print(this MockFileSystem src)
    {
        var root = src.DirectoryInfo.New("/");
        var all = root.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var x in all)
        {
            var marker = x is IFileInfo ? "F" : "D";
            Console.WriteLine($"{marker} {x.FullName}");
        }
    }

    public static MockFileSystem Clone(this MockFileSystem src, Action<MockFileSystem>? action = null)
    {
        var fs = new MockFileSystem();
        foreach (var path in src.AllFiles)
        {
            var data = new MockFileData(src.GetFile(path).Contents);
            fs.AddFile(path, data);
        }

        action?.Invoke(fs);
        return fs;
    }

    public static void ShouldContainFilesAs(this MockFileSystem actual, MockFileSystem expected)
    {
        // read all contents; remove "C:" and replace windows slashes to unix
        var actualDict = actual.AllFiles.ToDictionary(x => x.Substring(2).Replace('\\', '/'), x => actual.GetFile(x).GetHash());
        var expectedDict = expected.AllFiles.ToDictionary(x => x.Substring(2).Replace('\\', '/'), x => expected.GetFile(x).GetHash());

        actualDict.Should().Contain(expectedDict, SerializedInfo(actualDict, expectedDict));
    }

    public static void ShouldHaveSameFilesAs(this MockFileSystem actual, MockFileSystem expected)
    {
        // read all contents; remove "C:" and replace windows slashes to unix
        var actualDict = actual.AllFiles.ToDictionary(x => x.Substring(2).Replace('\\', '/'), x => actual.GetFile(x).GetHash());
        var expectedDict = expected.AllFiles.ToDictionary(x => x.Substring(2).Replace('\\', '/'), x => expected.GetFile(x).GetHash());

        actualDict.Should().BeEquivalentTo(expectedDict, SerializedInfo(actualDict, expectedDict));
    }

    public static TestFile InitFile(this MockFileSystem fs) => new TestFile(fs);

    public static TestFile LoadFile(this MockFileSystem fs, string path, string location)
    {

        if (fs.Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("path should be relative");
        }

        if (path.Contains('\\'))
        {
            throw new ArgumentException("use unix directory separators");
        }

        var actualFile = new FileInfo(fs.Path.Combine("Files", path));
        if (!actualFile.Exists)
        {
            throw new ArgumentException($"Test file not found: [{actualFile.FullName}]");
        }

        var data = new MockFileData(File.ReadAllBytes(actualFile.FullName));
        var tokens = path.Split('/').ToList();
        return InitFile(fs).Data(data).Name(tokens[^1]).In(location);
    }

    public static TestFile LoadFile(this MockFileSystem fs, string path, string location, MockFileData data)
    {

        if (fs.Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("path should be relative");
        }

        if (path.Contains('\\'))
        {
            throw new ArgumentException("use unix directory separators");
        }

        var tokens = path.Split('/').ToList();
        return InitFile(fs).Data(data).Name(tokens[^1]).In(string.Join('/', tokens[..^1].Prepend(location)));
    }

    public static List<TestFile> LoadDirectory(this MockFileSystem fs, string path, string location)
    {
        if (fs.Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("path should be relative");
        }

        if (path.Contains('\\'))
        {
            throw new ArgumentException("use unix directory separators");
        }

        var actualDir = new DirectoryInfo(fs.Path.Combine("Files", path));
        if (!actualDir.Exists)
        {
            throw new ArgumentException($"Test directory not found: [{actualDir.FullName}]");
        }

        var dirName = actualDir.Name;
        var targetLocation = fs.Path.Combine(location, dirName).Replace('\\', '/');

        var files = actualDir.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(x => new{Name=x.FullName.Replace(actualDir.FullName, "")[1..].Replace('\\', '/'), Data=new MockFileData(File.ReadAllBytes(x.FullName))})
            .ToList();
        var result = new List<TestFile>();
        foreach (var file in files)
        {
            result.Add(LoadFile(fs, file.Name, targetLocation, file.Data));
        }

        return result;
    }

    /// <summary>
    /// Combines several dictionaries. Throws on duplicate keys
    /// </summary>
    public static IDictionary<T1, T2> Combine<T1, T2>(IDictionary<T1, T2>[] sources)
        where T1 : notnull =>
        sources.SelectMany(dict => dict).ToLookup(pair => pair.Key, pair => pair.Value).ToDictionary(group => group.Key, group => group.Single());

    public static string GetHash(this MockFileData x) => GetHash(x.Contents);

    public static string GetHash(byte[] x, int maxLength=8)
    {
        if (x.Length == 0)
        {
            return "(empty)";
        }
        using var sha = SHA256.Create();
        var hashValue = sha.ComputeHash(x);
        var hash = BitConverter.ToString(hashValue).Replace("-", "")[..maxLength];
        return hash;
    }

    public static string SerializedInfo(Dictionary<string, string> actual, Dictionary<string, string> expected)
    {
        /*return @$"
Actual:
{SerializeDictionary(actual)}
Expected:
{SerializeDictionary(expected)}
";*/

        string GetValue(Dictionary<string, string> d, string key)
        {
            if (d.TryGetValue(key, out var value))
            {
                return value;
            }

            return " ░░░ "; // visually highlight nonexistent files
        }

        var allKeys = actual.Keys.Concat(expected.Keys).Distinct();
        var table = allKeys.Select(x => new Row(x, GetValue(expected, x), GetValue(actual, x))).ToList();
        var minLength = 10;
        var keyLength = table.Select(x => x.key.Length)
            .Concat(new[]
            {
                minLength
            })
            .Max();
        var actLength = table.Select(x => x.act.Length)
            .Concat(new[]
            {
                minLength
            })
            .Max();
        var expLength = table.Select(x => x.exp.Length)
            .Concat(new[]
            {
                minLength
            })
            .Max();
        var sb = new StringBuilder("ALL FILES:\n");

        void Serialize(Row x)
        {
            var key = x.key.PadRight(keyLength);
            var act = x.act.PadRight(actLength);
            var exp = x.exp.PadRight(expLength);
            var ok = x.act == x.exp
                ? "✓"
                : " ";
            sb.AppendLine(CultureInfo.InvariantCulture, $"{key}║{exp}║{act}║{ok}");
        }

        Serialize(new Row("PATH", $"EXPCTD {expected.Count}", $"ACTUAL {actual.Count}"));
        var groups = table.GroupBy(x => x.act == x.exp);
        foreach (var rows in groups.OrderBy(g => g.Key.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine(new string('═', keyLength + actLength + expLength + 4));

            var sorted = rows.OrderBy(x => Path.GetDirectoryName(x.key), StringComparer.OrdinalIgnoreCase).ThenBy(x => Path.GetFileName(x.key), StringComparer.OrdinalIgnoreCase);

            foreach (var x in sorted)
            {
                Serialize(x);
            }
        }

        sb.AppendLine(new string('═', keyLength + actLength + expLength + 4));

        return sb.ToString();
    }

    public record Row(string key, string exp, string act);
}
