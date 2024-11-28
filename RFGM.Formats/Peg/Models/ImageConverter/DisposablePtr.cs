using System.Runtime.InteropServices;

namespace RFGM.Formats.Peg.Models.ImageConverter;

class DisposablePtr(int length)
    : IDisposable
{
    public readonly IntPtr value = Marshal.AllocHGlobal(length);

    public void Dispose()
    {
        Marshal.FreeHGlobal(value);
    }
}
