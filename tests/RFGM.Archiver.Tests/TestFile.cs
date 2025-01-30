using System.IO.Abstractions.TestingHelpers;

namespace RFGM.Archiver.Tests;

public class TestFile
{
    private readonly MockFileSystem fs;

    private MockFileData fileData = new(string.Empty);

    private string name = string.Empty;
    private string fullPath = string.Empty;

    public TestFile(MockFileSystem fs) => this.fs = fs;

    public TestFile Data(string data)
    {
        fileData = new MockFileData(data);
        return this;
    }

    public TestFile Data(MockFileData data)
    {
        fileData = data;
        return this;
    }

    public TestFile Name(string value)
    {
        this.name = value;
        return this;
    }

    public void Delete() => fs.RemoveFile(fullPath);

    public TestFile In(string absPath)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Place file after initializing name and data");
        }

        fullPath = fs.Path.Combine(absPath, name);
        fs.AddFile(fullPath, fileData);
        return this;
    }
}
