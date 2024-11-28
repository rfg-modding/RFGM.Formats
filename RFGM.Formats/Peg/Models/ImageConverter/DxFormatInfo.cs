namespace RFGM.Formats.Peg.Models.ImageConverter;

record DxFormatInfo(Silk.NET.DXGI.Format DxFormat, bool Compressed, bool Extended, int CompressionRatio);
