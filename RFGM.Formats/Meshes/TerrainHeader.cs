using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

public struct TerrainHeader
{
    public uint Signature;
    public uint Version;
    public uint NumTextureNames;
    public uint TextureNamesSize;
    public uint NumFmeshNames;
    public uint FmeshNamesSize;
    public uint StitchPieceNamesSize;
    public uint NumStitchPieceNames;
    public uint NumStitchPieces;

    private const long SizeInFile = 36;

    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;
#endif

        Signature = stream.ReadUInt32();
        Version = stream.ReadUInt32();
        NumTextureNames = stream.ReadUInt32();
        TextureNamesSize = stream.ReadUInt32();
        NumFmeshNames = stream.ReadUInt32();
        FmeshNamesSize = stream.ReadUInt32();
        StitchPieceNamesSize = stream.ReadUInt32();
        NumStitchPieceNames = stream.ReadUInt32();
        NumStitchPieces = stream.ReadUInt32();

#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for TerrainHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}