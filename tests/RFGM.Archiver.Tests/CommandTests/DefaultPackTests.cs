using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NLog;

namespace RFGM.Archiver.Tests.CommandTests;

public class DefaultPackTests
{
    [TestCase("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "pack_default/cloth_sim.vpp_pc", null, TestName = "nested str2 with double extension, eg .sim_pc.str2_pc")]
    [TestCase("unpack_default/dlcp01_activities {vppMode=normal}.vpp_pc", "pack_default/dlcp01_activities.vpp_pc", null, TestName = "nested str2 and case-sensitive filenames")]
    [TestCase("unpack_default/dlc01_l1 {vppMode=normal}.vpp_pc", "pack_default/dlc01_l1.vpp_pc", null, TestName = "contains only .asm")]
    [TestCase("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "pack_default/xray_effect.str2_pc", null, TestName = "str2 with nested textures, mip level > 1")]
    [TestCase("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "pack_default/xray_effect.cpeg_pc", "pack_default/xray_effect.gpeg_pc", TestName = "pack peg, remove input properties")]
    [TestCase("unpack_default/xray_effect {pegAlign=16}.cpeg_pc", "pack_default/xray_effect.cpeg_pc", "pack_default/xray_effect.gpeg_pc", TestName = "pack peg without any properties")]
    public async Task SingleInput_DefaultOutput(string input, string outputPrimary, string? outputSecondary)
    {
        var name = input.Split('/').Last();
        var dirName = $"test/{name}";
        var fs = new MockFileSystem();
        fs.LoadDirectory(input, "test");
        // delete recursive stuff
        if (fs.Directory.Exists($"{dirName}/.unpack"))
        {
            fs.Directory.Delete($"{dirName}/.unpack", true);
        }

        var expected = fs.Clone();
        expected.LoadFile(outputPrimary, "test/.pack");
        if (outputSecondary is not null)
        {
            expected.LoadFile(outputSecondary, "test/.pack");
        }

        var code = await Program.RunMain([dirName], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task MultipleInput_DefaultOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test");
        fs.LoadDirectory("unpack_default/xray_effect {pegAlign=16}.cpeg_pc", "test");
        // delete recursive stuff
        if (fs.Directory.Exists("test/unpack_default/cloth_sim {vppMode=normal}.vpp_pc/.unpack"))
        {
            fs.Directory.Delete("test/unpack_default/cloth_sim {vppMode=normal}.vpp_pc/.unpack", true);
        }
        if (fs.Directory.Exists("test/unpack_default/xray_effect {pegAlign=16}.cpeg_pc/.unpack"))
        {
            fs.Directory.Delete("test/unpack_default/xray_effect {pegAlign=16}.cpeg_pc/.unpack", true);
        }

        var expected = fs.Clone();
        expected.LoadFile("pack_default/cloth_sim.vpp_pc", "test/.pack");
        expected.LoadFile("pack_default/xray_effect.cpeg_pc", "test/.pack");
        expected.LoadFile("pack_default/xray_effect.gpeg_pc", "test/.pack");


        var code = await Program.RunMain(["test/cloth_sim {vppMode=normal}.vpp_pc", "test/xray_effect {pegAlign=16}.cpeg_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task GpuInput_DefaultOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/xray_effect {pegAlign=16}.cpeg_pc", "test");
        fs.Directory.Move("test/xray_effect {pegAlign=16}.cpeg_pc", "test/xray_effect {pegAlign=16}.gpeg_pc");

        var expected = fs.Clone();
        expected.LoadFile("pack_default/xray_effect.cpeg_pc", "test/.pack");
        expected.LoadFile("pack_default/xray_effect.gpeg_pc", "test/.pack");

        var code = await Program.RunMain(["test/xray_effect {pegAlign=16}.gpeg_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task RelativePath_Ok()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/foo");

        var expected = fs.Clone();
        expected.LoadFile("pack_default/cloth_sim.vpp_pc", "test/foo/.pack");

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain(["../foo/cloth_sim {vppMode=normal}.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task RelativePath_NonexistentNoOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/foo");

        var expected = fs.Clone();

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain(["foo/cloth_sim {vppMode=normal}.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.NoOutput);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("c:/")]
    [TestCase("c:\\")]
    [TestCase("/")]
    public async Task AbsolutePath_Ok(string root)
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/foo");

        var expected = fs.Clone();
        expected.LoadFile("pack_default/cloth_sim.vpp_pc", "test/foo/.pack");

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain([$"{root}test/foo/cloth_sim {{vppMode=normal}}.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task NoForce_NoOverwrite_Error()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test");
        fs.LoadFile("pack_default/cloth_sim.vpp_pc", "test/.pack");
        var expected = fs.Clone();

        var code = await Program.RunMain(["test/cloth_sim {vppMode=normal}.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.FailedTasks);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task NoForce_NoOverwriteAddMissing_Error()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test");
        fs.LoadDirectory("unpack_default/dlc01_l1 {vppMode=normal}.vpp_pc", "test");
        fs.AddFile("test/.pack/cloth_sim.vpp_pc", new MockFileData([1,2,3]));

        var expected = fs.Clone();
        expected.LoadFile("pack_default/dlc01_l1.vpp_pc", "test/.pack");


        var code = await Program.RunMain(["test/cloth_sim {vppMode=normal}.vpp_pc", "test/dlc01_l1 {vppMode=normal}.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.FailedTasks);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Force_Overwrite_Ok()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test");
        fs.LoadDirectory("unpack_default/dlc01_l1 {vppMode=normal}.vpp_pc", "test");
        fs.AddFile("test/.pack/cloth_sim.vpp_pc", new MockFileData([1,2,3]));

        var expected = fs.Clone();
        expected.LoadFile("pack_default/cloth_sim.vpp_pc", "test/.pack");
        expected.LoadFile("pack_default/dlc01_l1.vpp_pc", "test/.pack");


        var code = await Program.RunMain(["test/cloth_sim {vppMode=normal}.vpp_pc", "test/dlc01_l1 {vppMode=normal}.vpp_pc", "-f"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase]
    public async Task NoProps_Error()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test");
        fs.Directory.Move("test/cloth_sim {vppMode=normal}.vpp_pc", "test/foo.vpp_pc");

        var expected = fs.Clone();

        var code = await Program.RunMain(["test/foo.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.FailedTasks);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("")]
    [TestCase("/")]
    [TestCase("\\")]
    public async Task TrailingSlash_Ok(string trailer)
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test");

        var expected = fs.Clone();
        expected.LoadFile("pack_default/cloth_sim.vpp_pc", "test/.pack");

        var code = await Program.RunMain(["test/cloth_sim {vppMode=normal}.vpp_pc" + trailer], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }
}
