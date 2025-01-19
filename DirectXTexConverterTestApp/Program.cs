using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RFGM.Formats.Peg;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Streams;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSimpleConsole());
services.AddSingleton<IPegArchiver, PegArchiver>();
services.AddSingleton<ImageConverter>();
services.AddSingleton<IFileSystem, FileSystem>();
var provider = services.BuildServiceProvider();
var archiver = provider.GetRequiredService<IPegArchiver>();
var converter = provider.GetRequiredService<ImageConverter>();
var fs = provider.GetRequiredService<IFileSystem>();
var log = provider.GetRequiredService<ILogger<Program>>();

// TODO REPLACE ME with some real cpeg+gpeg path
var pegFiles = PairedFiles.FromExistingFile(fs.FileInfo.New(@"C:\vault\rfg\peg_test\pegs\always_loaded.cpeg_pc"));
var archive = await archiver.UnpackPeg(pegFiles!.OpenRead(), "test", CancellationToken.None);
foreach (var logicalTexture in archive.LogicalTextures)
{
    log.LogInformation("{file} {texture}", pegFiles.FullName, logicalTexture);
    await Write(logicalTexture, CancellationToken.None);
}

async Task Write(LogicalTexture logicalTexture, CancellationToken token)
{
    var raw = new FileInfo($"_{logicalTexture.Name}.raw");
    log.LogDebug("Writing {name} {size}", raw.Name, logicalTexture.Data.Length);
    await using var s = raw.OpenWrite();
    await logicalTexture.Data.CopyToAsync(s, token);
    logicalTexture.Data.Seek(0, SeekOrigin.Begin);

    var dds = new FileInfo($"_{logicalTexture.Name}.dds");
    await using var d = dds.OpenWrite();
    var header = await converter.BuildHeader(logicalTexture, token);
    await header.CopyToAsync(d, token);
    await logicalTexture.Data.CopyToAsync(d, token);
    logicalTexture.Data.Seek(0, SeekOrigin.Begin);

    var png = new FileInfo($"_{logicalTexture.Name}.png");
    // TODO: crash
    var pngImage = converter.DecodeFirstFrame(logicalTexture);
    await using var pngOut = png.OpenWrite();
    await converter.WritePngFile(pngImage, pngOut, token);
}
