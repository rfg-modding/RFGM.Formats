using Hexa.NET.DirectXTex;

namespace RFGM.Formats.Peg.Models.ImageConverter;

class DisposableScratchImage(ScratchImage image) : IDisposable
{
    public void Dispose()
    {
        image.Release();
    }
}
