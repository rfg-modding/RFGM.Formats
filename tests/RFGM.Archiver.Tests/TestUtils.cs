using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RFGM.Archiver.Tests;

public static class TestUtils
{
    public static Action<HostBuilderContext, IServiceCollection> Hack(IFileSystem fs) =>
        (_, services) =>
        {
            services.Remove(services.Single(x => x.ServiceType == typeof(IFileSystem)));
            services.AddSingleton(fs);
        };
}