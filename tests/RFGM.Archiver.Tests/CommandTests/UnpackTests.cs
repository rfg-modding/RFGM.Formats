using System.IO.Abstractions.TestingHelpers;
using System.Xml.Linq;
using FluentAssertions;
using NLog;

namespace RFGM.Archiver.Tests.CommandTests;

public class UnpackTests
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
        var inName = primary.Split('/').Last();
        var outName = output.Split('/').Last();
        var fs = new MockFileSystem();
        fs.LoadFile(primary, "test");
        if (secondary is not null)
        {
            fs.LoadFile(secondary, "test");
        }

        var expected = fs.Clone();
        expected.LoadDirectory(output, "test/.unpack");
        var extra = expected.DirectoryInfo.New($"test/.unpack/{outName}/.unpack");
        if (extra.Exists)
        {
            extra.Delete(true);
        }

        var code = await Program.RunMain(["unpack", $"test/{inName}"], false, LogLevel.Trace, TestUtils.Hack(fs));

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
        var extra = expected.DirectoryInfo.New($"test/.unpack/")
            .EnumerateDirectories()
            .Select(x => x.EnumerateDirectories().FirstOrDefault(x => x.Name == ".unpack"))
            .Where(x => x != null);
        foreach (var e in extra)
        {
            e!.Delete(true);
        }

        var code = await Program.RunMain([
            "unpack",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
            $"test/xray_effect {{index=1}}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("")]
    [TestCase("/")]
    [TestCase("\\")]
    public async Task DirectoryInput_DefaultOutput(string trailer)
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
        var extra = expected.DirectoryInfo.New($"test/.unpack/")
            .EnumerateDirectories()
            .Select(x => x.EnumerateDirectories().FirstOrDefault(x => x.Name == ".unpack"))
            .Where(x => x != null);
        foreach (var e in extra)
        {
            e!.Delete(true);
        }

        var code = await Program.RunMain([
            "unpack",
            $"test" + trailer,
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task MixedInput_DefaultOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "foo");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "foo/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "test/.unpack");
        var extra = expected.DirectoryInfo.New($"test/.unpack/").EnumerateDirectories()
            .Concat(expected.DirectoryInfo.New($"foo/.unpack/").EnumerateDirectories())
            .Select(x => x.EnumerateDirectories().FirstOrDefault(x => x.Name == ".unpack"))
            .Where(x => x != null);
        foreach (var e in extra)
        {
            e!.Delete(true);
        }

        var code = await Program.RunMain([
            "unpack",
            $"test",
            $"foo/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Gpu_NoOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();

        var code = await Program.RunMain([
            "unpack",
            $"test/xray_effect {{index=2}}.gpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.NoOutput);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task RelativePath_NonexistentNoOutput()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "foo/test");
        fs.LoadFile("xray_effect.str2_pc", "test/foo");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "foo/test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "foo/test");

        var expected = fs.Clone();

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain([
            "unpack",
            $"test",
            $"foo/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.NoOutput);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task RelativePath_Ok()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "foo/test");
        fs.LoadFile("xray_effect.str2_pc", "test/foo");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "foo/test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "foo/test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "foo/test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "test/foo/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "foo/test/.unpack");
        var extra = expected.DirectoryInfo.New($"foo/test/.unpack/").EnumerateDirectories()
            .Concat(expected.DirectoryInfo.New($"test/foo/.unpack/").EnumerateDirectories())
            .Select(x => x.EnumerateDirectories().FirstOrDefault(x => x.Name == ".unpack"))
            .Where(x => x != null);
        foreach (var e in extra)
        {
            e!.Delete(true);
        }

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain([
            "unpack",
            $"../../foo/test",
            $"../foo/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("c:/")]
    [TestCase("c:\\")]
    [TestCase("/")]
    public async Task AbsolutePath_Ok(string root)
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "foo/test");
        fs.LoadFile("xray_effect.str2_pc", "test/foo");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "foo/test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "foo/test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "foo/test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "test/foo/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "foo/test/.unpack");
        var extra = expected.DirectoryInfo.New($"foo/test/.unpack/").EnumerateDirectories()
            .Concat(expected.DirectoryInfo.New($"test/foo/.unpack/").EnumerateDirectories())
            .Select(x => x.EnumerateDirectories().FirstOrDefault(x => x.Name == ".unpack"))
            .Where(x => x != null);
        foreach (var e in extra)
        {
            e!.Delete(true);
        }

        fs.Directory.SetCurrentDirectory("test/bar");
        var code = await Program.RunMain([
            "unpack",
            $"{root}foo/test",
            $"{root}test/foo/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

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

        var code = await Program.RunMain(["unpack", "test/cloth_sim.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

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
        full.Directory.Delete("test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/.unpack", true);
        var changeFile = "/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/dlc01_dm_01 {index=3}.str2_pc";
        var backup = await full.File.ReadAllBytesAsync(changeFile);
        await full.File.WriteAllBytesAsync(changeFile, [1, 2, 3]);
        var fs = full.Clone();
        var expected = full.Clone();

        fs.File.Delete("/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/DLC_AD_07_09 {index=2}.str2_pc");

        var code = await Program.RunMain(["unpack", "test/dlcp01_activities.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.FailedTasks);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Force_Overwrite_Ok()
    {
        var full = new MockFileSystem();
        full.LoadFile("dlcp01_activities.vpp_pc", "test");
        full.LoadDirectory("unpack_default/dlcp01_activities {vppMode=normal}.vpp_pc", "test/.unpack");
        full.Print();
        full.Directory.Delete("test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/.unpack", true);
        var changeFile = "/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/dlc01_dm_01 {index=3}.str2_pc";
        var backup = await full.File.ReadAllBytesAsync(changeFile);
        await full.File.WriteAllBytesAsync(changeFile, [1, 2, 3]);
        var fs = full.Clone();
        var expected = full.Clone();
        await expected.File.WriteAllBytesAsync(changeFile, backup);

        fs.File.Delete("/test/.unpack/dlcp01_activities {vppMode=normal}.vpp_pc/DLC_AD_07_09 {index=2}.str2_pc");

        var code = await Program.RunMain(["unpack", "-f", "test/dlcp01_activities.vpp_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("-r")]
    [TestCase("-d")]
    public async Task Recursive(string flag)
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "foo");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "foo/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            flag,
            $"test",
            $"foo/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Output_Relative()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "foo");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/result");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "foo/result");
        expected.LoadDirectory("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "test/result");

        var code = await Program.RunMain([
            "unpack",
            "-r",
            "-o",
            "result",
            $"test",
            $"foo/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("c:/")]
    [TestCase("c:\\")]
    [TestCase("/")]
    public async Task Output_Absolute(string root)
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "foo");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "/result");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "/result");
        expected.LoadDirectory("unpack_default/xray_effect {index=1, pegAlign=16}.cpeg_pc", "/result");

        var code = await Program.RunMain([
            "unpack",
            "-r",
            "-o",
            $"{root}result",
            $"test",
            $"foo/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("-l")]
    [TestCase("-d")]
    public async Task Localization_Flag_Convert(string flag)
    {
        var fs = new MockFileSystem();
        fs.LoadFile("locatexts_ar_eg {index=0}.rfglocatext", "test");

        var expected = fs.Clone();
        expected.LoadFile("unpack/locatexts_ar_eg {index=0}.rfglocatext_xml", "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            flag,
            "test/locatexts_ar_eg {index=0}.rfglocatext",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Localization_NoFlag_NoConvert()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("locatexts_ar_eg {index=0}.rfglocatext", "test");

        var expected = fs.Clone();
        expected.LoadFile("locatexts_ar_eg {index=0}.rfglocatext", "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            "test/locatexts_ar_eg {index=0}.rfglocatext",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("weapons {index=406}.xtbl")]
    [TestCase("default {index=743}.gtodx")]
    [TestCase("default {index=783}.dtodx")]
    public async Task Xml_Flag_Convert(string input)
    {
        var fs = new MockFileSystem();
        fs.LoadFile(input, "test");

        var expected = fs.Clone();
        var result = fs.Path.ChangeExtension(input, ".xml");
        expected.LoadFile($"unpack/{result}", "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            "-x",
            $"test/{input}",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("weapons {index=406}.xtbl")]
    [TestCase("default {index=743}.gtodx")]
    [TestCase("default {index=783}.dtodx")]
    public async Task Xml_NoFlag_NoConvert(string input)
    {
        var fs = new MockFileSystem();
        fs.LoadFile(input, "test");

        var expected = fs.Clone();
        expected.LoadFile(input, "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            $"test/{input}",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [TestCase("raw")]
    [TestCase("png")]
    [TestCase("dds")]
    public async Task Texture_Format_Convert(string format)
    {
        var fs = new MockFileSystem();
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory($"unpack/{format}/xray_effect {{index=1, pegAlign=16}}.cpeg_pc", "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            "-t",
            format,
            "test/xray_effect {index=1}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Texture_Format_PngDefault()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory($"unpack/png/xray_effect {{index=1, pegAlign=16}}.cpeg_pc", "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            "test/xray_effect {index=1}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task SkipContainers()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "test/.unpack");
        var extra = expected.DirectoryInfo.New($"test/.unpack/").EnumerateDirectories()
            .Select(x => x.EnumerateDirectories().FirstOrDefault(x => x.Name == ".unpack"))
            .Where(x => x != null);
        foreach (var e in extra)
        {
            e!.Delete(true);
        }
        var filterExtensions = new HashSet<string>() {".str2_pc", ".cpeg_pc", ".gpeg_pc"};
        var extraFiles = expected.DirectoryInfo.New($"test/.unpack/")
            .EnumerateDirectories()
            .SelectMany(x => x.EnumerateFiles().Where(x => filterExtensions.Contains(x.Extension)));
        foreach (var e in extraFiles)
        {
            e.Delete();
        }

        var code = await Program.RunMain([
            "unpack",
            "-s",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task SkipContainers_Recursive()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "test");

        var expected = fs.Clone();
        expected.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test/.unpack");
        expected.LoadDirectory("unpack_default/xray_effect {vppMode=compacted}.str2_pc", "test/.unpack");
        var filterExtensions = new HashSet<string>() {".str2_pc", ".cpeg_pc", ".gpeg_pc"};
        var extraFiles = expected.DirectoryInfo.New($"test/.unpack/")
            .EnumerateDirectories()
            .SelectMany(x => x.EnumerateFiles().Where(x => filterExtensions.Contains(x.Extension)));
        foreach (var e in extraFiles)
        {
            e.Delete();
        }

        var code = await Program.RunMain([
            "unpack",
            "-sr",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Metadata()
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
        expected.LoadFile("unpack/metadata/.metadata.csv", "test/.unpack");
        var extra = expected.DirectoryInfo.New($"test/.unpack/")
            .EnumerateDirectories()
            .Select(x => x.EnumerateDirectories().FirstOrDefault(x => x.Name == ".unpack"))
            .Where(x => x != null);
        foreach (var e in extra)
        {
            e!.Delete(true);
        }

        var code = await Program.RunMain([
            "unpack",
            "-m",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
            $"test/xray_effect {{index=1}}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Metadata_Recursive()
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
        expected.LoadFile("unpack/metadata_recursive/.metadata.csv", "test/.unpack");

        var code = await Program.RunMain([
            "unpack",
            "-mr",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
            $"test/xray_effect {{index=1}}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Filter_Empty()
    {
        var fs = new MockFileSystem();
        fs.LoadFile("cloth_sim.vpp_pc", "test");
        fs.LoadFile("xray_effect.str2_pc", "test");
        fs.LoadFile("xray_effect {index=1}.cpeg_pc", "test");
        fs.LoadFile("xray_effect {index=2}.gpeg_pc", "test");

        var expected = fs.Clone();

        var code = await Program.RunMain([
            "unpack",
            "-r",
            "--filter",
            "",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
            $"test/xray_effect {{index=1}}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.NoOutput);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Filter_Any()
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

        var code = await Program.RunMain([
            "unpack",
            "-r",
            "--filter",
            "**/*",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
            $"test/xray_effect {{index=1}}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }

    [Test]
    public async Task Filter_Extension()
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
        var extra = expected.DirectoryInfo.New($"test/.unpack/")
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(x => !x.Name.EndsWith(".sim_pc"));
        foreach (var e in extra)
        {
            e.Delete();
        }


        var code = await Program.RunMain([
            "unpack",
            "-r",
            "--filter",
            "**/*.sim_pc",
            $"test/cloth_sim.vpp_pc",
            $"test/xray_effect.str2_pc",
            $"test/xray_effect {{index=1}}.cpeg_pc",
        ], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }
}
