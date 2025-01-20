using System.Runtime.InteropServices;

namespace RFGM.Formats.Peg.Models.ImageConverter;

class DisposablePtr(int length)
    : IDisposable
{
    public readonly nint Value = Marshal.AllocHGlobal(length);

    public void Dispose()
    {
        Marshal.FreeHGlobal(Value);
    }
}
