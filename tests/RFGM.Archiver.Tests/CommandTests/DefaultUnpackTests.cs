using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NLog;

namespace RFGM.Archiver.Tests.CommandTests;

public class DefaultUnpackTests
{
    [TestCase("cloth_sim.vpp_pc", null, "unpack_default/cloth_sim {vppMode=normal}.vpp_pc", TestName = "nested str2 with double extension, eg .sim_pc.str2_pc")]
    [TestCase("dlcp01_activities.vpp_pc", null, "unpack_default/dlcp01_activities {vppMode=normal}.vpp_pc", TestName = "nested str2 and case-sensitive filenames")]
    [TestCase("dlc01_l1.vpp_pc", null, "unpack_default/dlc01_l1 {vppMode=normal}.vpp_pc", TestName = "contains only .asm")]
    [TestCase("xray_effect.str2_pc", null, "unpack_default/xray_effect {vppMode=compacted}.str2_pc", TestName = "str2 with nested textures, mip level > 1")]
    [TestCase("xray_effect {index=1}.cpeg_pc", "xray_effect {index=2}.gpeg_pc", "unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", TestName = "unpack peg, preserve input properties")]
    [TestCase("xray_effect.cpeg_pc", "xray_effect.gpeg_pc", "unpack_default/xray_effect {pegAlign=16}.cpeg_pc", TestName = "unpack peg without any properties")]
    [TestCase("dcl_mc_edfdirt_01_d {index=26}.cvbm_pc", "dcl_mc_edfdirt_01_d {index=27}.gvbm_pc", "unpack_default/dcl_mc_edfdirt_01_d {index=26, pegAlign=16}.cvbm_pc", TestName = "peg with another extension")]
    public async Task SingleInput_DefaultOutput(string primary, string? secondary, string output)
    {
        var fs = new MockFileSystem();
        fs.LoadFile(primary, "test");
        if (secondary is not null)
        {
            fs.LoadFile(secondary, "test");
        }

        var expected = fs.Clone();
        expected.LoadDirectory(output, "test/.unpack");
        if (primary == "xray_effect.str2_pc")
        {
            // workaround for very long path git error
            expected.LoadDirectory("xray_peg/xray_effect {index=1, pegAlign=16}.cpeg_pc", "test/.unpack/xray_effect {vppMode=compacted}.str2_pc/.unpack");
        }

        var code = await Program.RunMain([fs.AllFiles.First()], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task MultipleInput_DefaultOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "test");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");
        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "test/.unpack");
        // workaround for very long path git error
        expected.LoadDirectory("xray_peg/xray_effect {index=1, pegAlign=16}.cpeg_pc", "test/.unpack/xray_effect {vppMode=compacted}.str2_pc/.unpack");

        // ignore gpeg
        var code = await Program.RunMain(fs.AllFiles.ToArray()[..^1], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("xray_effect.gpeg_pc", "xray_effect.cpeg_pc", null, TestName = "no output if started with GPU file")]
    public async Task Gpu_NoOutput(string primary, string? secondary, string? output)
    {
        var fs = new MockFileSystem();
        fs.LoadFile(primary, "test");
        if (secondary is not null)
        {
            fs.LoadFile(secondary, "test");
        }

        var expected = fs.Clone();
        if (output is not null)
        {
            expected.LoadDirectory(output, "test/.unpack");
        }

        var code = await Program.RunMain([fs.AllFiles.First()], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.NoOutput);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task RelativePath_Ok()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test/foo");
        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/foo/.unpack");

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain(["../foo/cloth_sim.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task RelativePath_NonexistentNoOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test/foo");
        var expected = fs.Clone();

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain(["foo/cloth_sim.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.NoOutput);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("c:/")]
    [TestCase("c:\\")]
    [TestCase("/")]
    public async Task AbsolutePath_Ok(string root)
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test/foo");
        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/foo/.unpack");

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain([$"{root}test/foo/cloth_sim.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task NoForce_NoOverwrite_Error()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/.unpack");
        var changeFile = "/test/.unpack/cloth_sim {vppMode=normal}.vpp_pc/.unpack/banner_bl.sim_pc {index=41, vppMode=compacted}.str2_pc/banner_bl {index=0}.sim_pc";
        var backup = await fs.File.ReadAllBytesAsync(changeFile);
        await fs.File.WriteAllBytesAsync(changeFile, [1, 2, 3]);

        var expected = fs.Clone();

        var code = await Program.RunMain(["test/cloth_sim.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.FailedTasks);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task NoForce_NoOverwriteAddMissing_Error()
    {
        var full = new MockFileSystem();
        full.LoadFile("dlcp01_activities.vpp_pc", "test");
        full.LoadDirectory("unpack_default/dlcp01_activities {vppMode=normal}.vpp_pc", "test/.unpack");
        full.Print();
        var changeFile = "/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/.unpack/DLC_AD_07_09 {index=2, vppMode=compacted}.str2_pc/dlc01_07_09_129 {index=0}.layer_pc";
        var backup = await full.File.ReadAllBytesAsync(changeFile);
        await full.File.WriteAllBytesAsync(changeFile, [1, 2, 3]);
        var fs = full.Clone();
        var expected = full.Clone();

        fs.File.Delete("/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/DLC_AD_07_09 {index=2}.str2_pc");
        fs.Directory.Delete("/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/.unpack/dlc01_dm_06 {index=7, vppMode=compacted}.str2_pc", true);

        var code = await Program.RunMain(["test/dlcp01_activities.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.FailedTasks);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Force_Overwrite_Ok()
    {
        var full = new MockFileSystem();
        full.LoadFile("dlcp01_activities.vpp_pc", "test");
        full.LoadDirectory("unpack_default/dlcp01_activities {vppMode=normal}.vpp_pc", "test/.unpack");
        var changeFile = "/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/.unpack/DLC_AD_07_09 {index=2, vppMode=compacted}.str2_pc/dlc01_07_09_129 {index=0}.layer_pc";
        var backup = await full.File.ReadAllBytesAsync(changeFile);
        await full.File.WriteAllBytesAsync(changeFile, [1, 2, 3]);
        var fs = full.Clone();
        var expected = full.Clone();
        await expected.File.WriteAllBytesAsync(changeFile, backup);

        fs.File.Delete("/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/DLC_AD_07_09 {index=2}.str2_pc");
        fs.Directory.Delete("/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/.unpack/dlc01_dm_06 {index=7, vppMode=compacted}.str2_pc", true);

        var code = await Program.RunMain(["test/dlcp01_activities.vpp_pc", "-f"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }
}
