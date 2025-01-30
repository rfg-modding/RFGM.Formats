using Hexa.NET.DirectXTex;
using Microsoft.Extensions.Logging;
using RFGM.Formats.Peg.Models;
using RFGM.Formats.Peg.Models.ImageConverter;
using RFGM.Formats.Streams;
using Silk.NET.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Image = Hexa.NET.DirectXTex.Image;

namespace RFGM.Formats.Peg;

/// <summary>
/// Utility to convert any DDS format used in RFGR to PNG and back
/// </summary>
public class ImageConverter(ILogger<ImageConverter> log)
{
    public async Task<Stream> ImageToTexture(Stream image, ImageFormat imageFormat, LogicalTexture logicalTexture, CancellationToken token)
    {
        log.LogDebug("Converting [{name}] from {format} to texture", logicalTexture.Name, imageFormat);
        if (!image.CanSeek)
        {
            throw new ArgumentException($"Need seekable stream, got {image}", nameof(image));
        }

        if (!image.CanRead)
        {
            throw new ArgumentException($"Need readable stream, got {image}", nameof(image));
        }

        if (image.Position != 0)
        {
            throw new ArgumentException($"Expected start of stream, got position = {image.Position}", nameof(image));
        }

        switch (imageFormat)
        {
            case ImageFormat.dds:
                // blindly cut header and copy data
                var header = await BuildHeader(logicalTexture, token);
                var ms = new MemoryStream();
                image.Seek(header.Length, SeekOrigin.Begin);
                await image.CopyToAsync(ms, token);
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            case ImageFormat.png:
                var pngImage = await PngDecoder.Instance.DecodeAsync<Rgba32>(new PngDecoderOptions(), image, token);
                if (pngImage.Width != logicalTexture.Size.Width || pngImage.Height != logicalTexture.Size.Height)
                {
                    throw new InvalidOperationException($"PNG has size {pngImage.Width}x{pngImage.Height}, expected {logicalTexture.Size}");
                }
                return EncodeToRaw(pngImage, logicalTexture);
            case ImageFormat.raw:
                return image.MakeDeepOwnCopy();
            default:
                throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null);
        }
    }

    public async Task<Stream> TextureToImage(LogicalTexture logicalTexture, ImageFormat imageFormat, CancellationToken token)
    {
        log.LogDebug("Converting [{name}] from texture to {format}", logicalTexture.Name, imageFormat);
        if (!logicalTexture.Data.CanSeek)
        {
            throw new ArgumentException($"Need seekable stream, got {logicalTexture.Data}", nameof(logicalTexture.Data));
        }

        if (!logicalTexture.Data.CanRead)
        {
            throw new ArgumentException($"Need readable stream, got {logicalTexture.Data}", nameof(logicalTexture.Data));
        }

        if (logicalTexture.Data.Position != 0)
        {
            throw new ArgumentException($"Expected start of stream, got position = {logicalTexture.Data.Position}", nameof(logicalTexture.Data));
        }

        switch (imageFormat)
        {
            case ImageFormat.dds:
                var header = await BuildHeader(logicalTexture, token);
                var ms = new MemoryStream();
                await header.CopyToAsync(ms, token);
                await logicalTexture.Data.CopyToAsync(ms, token);
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            case ImageFormat.png:
                var pngImage = DecodeDDSFirstFrame(logicalTexture);
                var encoder = new PngEncoder();
                var ms2 = new MemoryStream();
                await encoder.EncodeAsync(pngImage, ms2, token);
                ms2.Seek(0, SeekOrigin.Begin);
                return ms2;
            case ImageFormat.raw:
                return logicalTexture.Data;
            default:
                throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null);
        }
    }

    public unsafe Stream EncodeToRaw(Image<Rgba32> png, LogicalTexture logicalTexture)
    {
        using var disposables = new Disposables();
        var pngSize = png.Width * png.Height * (png.PixelType.BitsPerPixel / 8);
        var pointer = new DisposablePtr(pngSize);
        disposables.Add(pointer);
        var span = new Span<byte>(pointer.Value.ToPointer(), pngSize);
        png.CopyPixelDataTo(span);
        var initialFormat = (int)Format.FormatR8G8B8A8Unorm;
        nuint rowPitch = 0;
        nuint slicePitch = 0;
        DirectXTex.ComputePitch(initialFormat, (nuint) png.Width, (nuint) png.Height, ref rowPitch, ref slicePitch, CPFlags.None);
        var dxImage = new Image((nuint) png.Width, (nuint) png.Height, initialFormat, rowPitch, (nuint)pngSize, (byte*) pointer.Value.ToPointer());
        var scratchImage = DirectXTex.CreateScratchImage();
        scratchImage.InitializeFromImage(dxImage, false, CPFlags.None);
        var scratchSize = (int)scratchImage.GetPixelsSize();
        if (scratchSize != pngSize)
        {
            throw new InvalidOperationException($"Failed to initialize image. Pixel sizes: png={pngSize} scratch={scratchSize}");
        }
        disposables.Add(new DisposableScratchImage(scratchImage));

        if (logicalTexture.MipLevels > 1)
        {
            var newImage = DirectXTex.CreateScratchImage();
            DirectXTex.GenerateMipMaps(scratchImage.GetImages(), TexFilterFlags.Default, (nuint) logicalTexture.MipLevels, ref newImage, false);
            disposables.Add(new DisposableScratchImage(newImage));
            var originalSize = (int)scratchImage.GetPixelsSize();
            var newSize = (int)newImage.GetPixelsSize();
            var expectedSize = GetByteSizeWithMips(originalSize, logicalTexture.MipLevels);
            if (newSize!= expectedSize)
            {
                throw new InvalidOperationException($"Failed to generate mips. Pixel sizes: original={originalSize} new={newSize} expected={expectedSize}");
            }
            scratchImage = newImage;
        }
        var (dxFormat, compressed, _, compressionRatio) = GetDxFormat(logicalTexture.Format, logicalTexture.Flags);
        if (compressed)
        {
            var newImage = DirectXTex.CreateScratchImage();
            var metadata = scratchImage.GetMetadata();
            DirectXTex.Compress2(scratchImage.GetImages(), scratchImage.GetImageCount(), ref metadata, (int) dxFormat, TexCompressFlags.Default, 0.5f, ref newImage);
            //DirectXTex.Compress(scratchImage.GetImages(), (int) dxFormat, TexCompressFlags.Default, 0.5f, ref newImage);
            var originalSize = (int)scratchImage.GetPixelsSize();
            var newSize = (int)newImage.GetPixelsSize();
            var expectedSize = originalSize / compressionRatio;
            if (newSize!= expectedSize)
            {
                throw new InvalidOperationException($"Failed to compress. Pixel sizes: original={originalSize} new={newSize} expected={expectedSize}");
            }
            disposables.Add(new DisposableScratchImage(newImage));
            scratchImage = newImage;
        }

        if (dxFormat == Format.FormatR8G8B8A8UnormSrgb)
        {
            var newImage = DirectXTex.CreateScratchImage();
            DirectXTex.Convert(scratchImage.GetImages(), (int) Format.FormatR8G8B8A8UnormSrgb, TexFilterFlags.Default, 0.5f, ref newImage);
            log.LogTrace("Size after conversion to {format}: {pixels}", dxFormat, newImage.GetPixelsSize());
            disposables.Add(new DisposableScratchImage(newImage));
            scratchImage = newImage;
        }

        var dxSize = (int)scratchImage.GetPixelsSize(); // total length with all mips
        log.LogTrace("Size after all manipulations: {pixels}", dxSize);
        var dataAlign = logicalTexture.Align;
        var remainder = dxSize % dataAlign;
        var padding = remainder > 0 ? dataAlign - remainder : 0;
        var totalSize = dxSize + padding;
        log.LogTrace("DDS DATA padding {padding}, totalSize {totalSize}", padding, totalSize);
        // TODO maybe return unmanaged memory stream and make 1 less copy?
        var pixels = scratchImage.GetPixels();
        var pixelSpan = new Span<byte>(pixels, dxSize);
        var result = new MemoryStream(dxSize);
        result.Write(pixelSpan);
        var padSpan = new byte[padding].AsSpan();
        padSpan.Fill(0);
        result.Write(padSpan);
        result.Position = 0;
        return result;
    }

    public unsafe Image<Rgba32> DecodeDDSFirstFrame(LogicalTexture logicalTexture)
    {
        using var disposables = new Disposables();
        var header = BuildHeader(logicalTexture, CancellationToken.None).Result;
        disposables.Add(header);
        var ddsFileSize = (int)header.Length + logicalTexture.TotalSize;
        var pointer = new DisposablePtr(ddsFileSize);
        disposables.Add(pointer);
        using (var mem = new UnmanagedMemoryStream((byte*) pointer.Value.ToPointer(), ddsFileSize, ddsFileSize, FileAccess.Write))
        {
            header.CopyTo(mem);
            logicalTexture.Data.CopyTo(mem);
        }

        TexMetadata metadata = default;
        var scratchImage = DirectXTex.CreateScratchImage();
        DirectXTex.LoadFromDDSMemory(pointer.Value.ToPointer(), (UIntPtr)ddsFileSize, DDSFlags.None, ref metadata, ref scratchImage);
        disposables.Add(new DisposableScratchImage(scratchImage));
        var (_, compressed, _, _) = GetDxFormat(logicalTexture.Format, logicalTexture.Flags);
        if (compressed)
        {
            // block-compressed images can't be converted and have to be decompressed instead
            var newImage = DirectXTex.CreateScratchImage();
            DirectXTex.Decompress(scratchImage.GetImages(), (int) Format.FormatR8G8B8A8Unorm, ref newImage);
            disposables.Add(new DisposableScratchImage(newImage));
            scratchImage = newImage;
        }
        if (scratchImage.GetMetadata().Format != (int)Format.FormatR8G8B8A8Unorm)
        {
            // maybe it was uncompressed, eg r8g8b8a8_unorm_srgb. convert it to regular colorspace with default options
            var newImage = DirectXTex.CreateScratchImage();;
            DirectXTex.Convert(scratchImage.GetImages(), (int) Format.FormatR8G8B8A8Unorm, TexFilterFlags.Default, 0.5f, ref newImage);
            disposables.Add(new DisposableScratchImage(newImage));
            scratchImage = newImage;
        }

        var dxOutImage = scratchImage.GetImage(UIntPtr.Zero, UIntPtr.Zero, UIntPtr.Zero);
        var rowPitch = (int)dxOutImage->RowPitch;
        if (rowPitch != logicalTexture.Size.Width*4)
        {
            log.LogWarning("DirectX unpacked texture [{name}] pitch {pitch} != width {width} * 4. Possible data corruption when converting to PNG", logicalTexture.Name, rowPitch, logicalTexture.Size.Width);
        }

        var firstFrameByteSize = logicalTexture.Size.Width * logicalTexture.Size.Height * 4;
        var pixels = dxOutImage->Pixels;
        var span = new Span<byte>(pixels, firstFrameByteSize);
        return SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(span, logicalTexture.Size.Width, logicalTexture.Size.Height);
    }

    /// <summary>
    /// DDS header: 128 bytes for BC1/BC3, 148 bytes for SRGB variants and RGBA
    /// </summary>
    public async Task<Stream> BuildHeader(LogicalTexture logicalTexture, CancellationToken token)
    {
        var ms = new MemoryStream();
        await BinUtils.Write(ms, new byte[]{0x44, 0x44, 0x53, 0x20}, token); // "DDS " magic

        // DDS header
        var format = GetDxFormat(logicalTexture.Format, logicalTexture.Flags);
        var headerFlags = HeaderFlags.Required | HeaderFlags.MipmapCount;  // TODO mipmap level 1 needs special handling? maybe not
        //headerFlags |= logicalTexture.MipLevels > 1 ? HeaderFlags.MipmapCount : 0;
        headerFlags |= format.Compressed ? HeaderFlags.LinearSizeCompressed : HeaderFlags.PitchUncompressed;
        var caps = HeaderCaps.Mipmap | HeaderCaps.Texture | HeaderCaps.Complex;
        var rowPitch = UIntPtr.Zero;
        var slicePitch = UIntPtr.Zero;
        DirectXTex.ComputePitch((int)format.DxFormat, (UIntPtr)logicalTexture.Size.Width, (UIntPtr)logicalTexture.Size.Height, ref rowPitch, ref slicePitch, CPFlags.None);
        var frameSize = logicalTexture.Size.Width * logicalTexture.Size.Height * 4 / format.CompressionRatio;
        await BinUtils.WriteUint4(ms, 0x7c, token);
        await BinUtils.WriteUint4(ms, (uint)headerFlags, token);
        await BinUtils.WriteUint4(ms, logicalTexture.Size.Height, token);
        await BinUtils.WriteUint4(ms, logicalTexture.Size.Width, token);
        await BinUtils.WriteUint4(ms, format.Compressed ? frameSize : rowPitch.ToUInt32(), token);
        await BinUtils.WriteUint4(ms, 1, token); // depth
        await BinUtils.WriteUint4(ms, logicalTexture.MipLevels, token);
        await BinUtils.WriteZeroes(ms, 11*4, token); // reserved
        await BinUtils.WriteUint4(ms, 0x20, token); // pixel format struct size
        await BinUtils.WriteUint4(ms, (uint)PixelFormatFlags.FourCC, token);
        if (format.Extended)
        {
            await BinUtils.Write(ms, new byte[]{0x44, 0x58, 0x31, 0x30}, token); // "DX10"
        }
        else
        {
            var fourcc = format.DxFormat == Format.FormatBC1Unorm ? new byte[] {0x44, 0x58, 0x54, 0x31} : new byte[] {0x44, 0x58, 0x54, 0x35}; // "DXT1" or "DXT5"
            await BinUtils.Write(ms, fourcc, token);
        }
        await BinUtils.WriteZeroes(ms, 5*4, token);
        await BinUtils.WriteUint4(ms, (uint)caps, token);
        await BinUtils.WriteZeroes(ms, 4*4, token);

        if (format.Extended)
        {
            // DXT10 extension header for srgb and rgba8 formats
            await BinUtils.WriteUint4(ms, (uint) format.DxFormat, token);
            await BinUtils.WriteUint4(ms, (uint) TexDimension.Texture2D, token);
            await BinUtils.WriteUint4(ms, 0, token);
            await BinUtils.WriteUint4(ms, 1, token);
            await BinUtils.WriteUint4(ms, 0, token);
        }

        ms.Position = 0;
        return ms;
    }

    private DxFormatInfo GetDxFormat(RfgCpeg.Entry.BitmapFormat format, TextureFlags flags) =>
        format switch
        {
            RfgCpeg.Entry.BitmapFormat.PcDxt1 => flags.HasFlag(TextureFlags.Srgb) ? new DxFormatInfo(Format.FormatBC1UnormSrgb, true, true, 8) : new DxFormatInfo(Format.FormatBC1Unorm, true, false, 8),
            RfgCpeg.Entry.BitmapFormat.PcDxt5 => flags.HasFlag(TextureFlags.Srgb) ? new DxFormatInfo(Format.FormatBC3UnormSrgb, true, true, 4) : new DxFormatInfo(Format.FormatBC3Unorm, true, false, 4),
            RfgCpeg.Entry.BitmapFormat.Pc8888 => flags.HasFlag(TextureFlags.Srgb) ? new DxFormatInfo(Format.FormatR8G8B8A8UnormSrgb, false, true, 1) : new DxFormatInfo(Format.FormatR8G8B8A8Unorm, false, true, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
        };

    /// <summary>
    /// Geometric progression: each subsequent mip size is 1/4 of previous
    /// </summary>
    private int GetByteSizeWithMips(int size, int mipLevel)
    {
        return (int) (size * (1 - System.Math.Pow(0.25, mipLevel)) / (1 - 0.25));
    }
}
