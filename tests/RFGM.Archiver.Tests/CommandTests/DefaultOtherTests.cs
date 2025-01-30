using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NLog;
using NSubstitute;
using RFGM.Archiver.Services;

namespace RFGM.Archiver.Tests.CommandTests;

public class DefaultOtherTests
{
    [Test]
    public async Task BogusInput_NoOutput_Error()
    {
        var fs = new MockFileSystem();
        var expected = fs.Clone();

        var code = await Program.RunMain(["fake"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.NoOutput);
        fs.AllNodes.Should().BeEquivalentTo(expected.AllNodes);
    }

    [Test]
    public async Task Rick_Rolls()
    {
        var fs = new MockFileSystem();
        var expected = fs.Clone();

        var code = await Program.RunMain(["/somewhere/rfg.exe"], false, LogLevel.Trace, (context, services) =>
        {
            TestUtils.Hack(fs);
            services.Remove(services.Single(x => x.ServiceType == typeof(Rick)));
            services.AddSingleton(Substitute.For<Rick>(NullLogger<Rick>.Instance));
        });

        code.Should().Be(ExitCode.Rick);
        fs.AllNodes.Should().BeEquivalentTo(expected.AllNodes);
    }

    [Test]
    public async Task NoInput_Error()
    {
        var fs = new MockFileSystem();
        // expect default dirs like "c:/test"
        var expected = fs.Clone();

        var code = await Program.RunMain([], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.AllNodes.Should().BeEquivalentTo(expected.AllNodes);
    }

    [Test]
    public async Task MixedInput_Ok()
    {
        var fs = new MockFileSystem();
        fs.LoadDirectory("unpack_default/cloth_sim {vppMode=normal}.vpp_pc", "test");
        fs.LoadFile("xray_effect.cpeg_pc", "test");
        fs.LoadFile("xray_effect.gpeg_pc", "test");

        var expected = fs.Clone();
        expected.LoadFile("pack_default/cloth_sim.vpp_pc", "test/.pack");
        expected.LoadDirectory("unpack_default/xray_effect {pegAlign=16}.cpeg_pc", "test/.unpack");

        var code = await Program.RunMain(["test/cloth_sim {vppMode=normal}.vpp_pc", "test/xray_effect.cpeg_pc"], false, LogLevel.Trace, TestUtils.Hack(fs));

        code.Should().Be(ExitCode.Ok);
        fs.ShouldHaveSameFilesAs(expected);
    }
}
