using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using ICSharpCode.SharpZipLib;
using Kaitai;
using RFGM.Formats.Abstractions;
using RFGM.Formats.Abstractions.Descriptors;
using RFGM.Formats.Vpp.Models;

namespace RFGM.Formats.Vpp;

public class VppReader(OptimizeFor profile)
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "I dont care about disposing wrappers")]
    public LogicalArchive Read(Stream source, string name, CancellationToken token)
    {
        var s = new KaitaiStream(source);
        var vpp = new RfgVpp(s);
        var originalMode = vpp.Header.Flags.Mode;
        var data = ReadData(vpp, token);
        return new LogicalArchive(data, originalMode, name, vpp.BlockOffset, vpp.Header.LenCompressedData);
    }

    /// <summary>
    /// CAREFUL: file stream is not explicitly closed
    /// </summary>
    public LogicalArchive ReadFile(IFileInfo file, CancellationToken token)
    {
        var entryInfo = FormatDescriptors.RegularFile.ReadEntryForEncoding(file);
        var f = file.OpenRead();
        return Read(f, entryInfo.Name, token);
    }

    private IEnumerable<LogicalFile> ReadData(RfgVpp vpp, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!vpp.Entries.Any())
        {
            yield break;
        }

        Action<OptimizeFor, CancellationToken> fixupAction = vpp.Header.Flags.Mode switch
        {
            RfgVpp.HeaderBlock.Mode.Normal => vpp.FixOffsetOverflow,
            RfgVpp.HeaderBlock.Mode.Compacted => vpp.ReadCompactedData,
            RfgVpp.HeaderBlock.Mode.Compressed => vpp.ReadCompressedData,
            RfgVpp.HeaderBlock.Mode.Condensed => throw new InvalidOperationException("Condensed-only mode is not present in vanilla files and is not supported"),
            _ => throw new ArgumentOutOfRangeException()
        };

        try
        {
            fixupAction.Invoke(profile, token);
        }
        catch (SharpZipBaseException e)
        {
            var streamSize = "(unknown)";
            if (vpp.M_Io.BaseStream is MemoryStream ms)
            {
                streamSize = ms.Length.ToString(CultureInfo.InvariantCulture);
            }

            var msg = $"Failed to unzip data. Stream size = [{streamSize}]. Header = [{vpp.Header}]";
            throw new InvalidOperationException(msg, e);
        }

        foreach (var entryData in vpp.BlockEntryData.Value)
        {
            token.ThrowIfCancellationRequested();
            yield return new LogicalFile(entryData.GetDataStream(), entryData.XName, entryData.I, entryData.ToString(), entryData.CompressedStream);
        }
    }
}
