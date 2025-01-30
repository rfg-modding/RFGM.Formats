using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NLog;

namespace RFGM.Archiver.Tests.CommandTests;

public class MetadataTests
{
    [Test]
    public async Task Metadata_Recursive_SavedInCurrentDirectory()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "test");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadFile("unpack/metadata_recursive/.metadata.csv", "/");

        var code = await Program.RunMain([
            "metadata",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
            $"test/xray_effect {{index=1}}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }
}
