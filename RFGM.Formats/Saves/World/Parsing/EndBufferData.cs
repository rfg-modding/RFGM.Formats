using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Parsing;

public class EndBufferData
{
    private int _length;
    private int _v13;
    private int _v5;
    private int _v14;

    private void CalcPadding(Stream stream)
    {
        _length = (int)stream.Position;
        _v13 = (_length + 16) % 512;
        _v5 = 512 - _v13;
        _v14 = _v5 + _length + 16;
    }

    public void Read(Stream stream)
    {
        CalcPadding(stream);
        stream.Seek(_v14, SeekOrigin.Begin);
    }

    public void Write(Stream stream)
    {
        CalcPadding(stream);
            
        if (_v14 >= 2621440) return;
            
        stream.Seek(0, SeekOrigin.Begin);
        var buffer = new byte[_v14 - 16];
        stream.ReadExactly(buffer, 0, _v14 - 16);
        var hashValue = EndBufferHash.ConsoleMd5Mem(buffer, (uint)(_v5 + _length));

        stream.Seek(_v5 + _length, SeekOrigin.Begin);
        stream.WriteUInt32(hashValue.Val0);
        stream.WriteUInt32(hashValue.Val1);
        stream.WriteUInt32(hashValue.Val2);
        stream.WriteUInt32(hashValue.Val3);
    }
}