using Silk.NET.DXGI;

namespace RFGM.Formats.Peg.Models.ImageConverter;

record DxFormatInfo(Format DxFormat, bool Compressed, bool Extended, int CompressionRatio);
