namespace RFGM.Formats.Peg.Models.ImageConverter;

class Disposables : List<IDisposable>, IDisposable
{
    public void Dispose()
    {
        Reverse();
        foreach (var item in this)
        {
            item.Dispose();
        }
    }
}