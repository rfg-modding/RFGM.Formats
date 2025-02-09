using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class VfxExpression
{
    public uint FunctionNameCrc;
    public uint JustAStub;
    public int ObjectIndex;
    public int NumExpressionParams;
    public uint ExpressionParamsOffset;
    
    public string FunctionName => HashDictionary.FindOriginString(FunctionNameCrc) ?? "Unknown";

    public List<VfxExpressionParam> Parameters = new();
    
    private const long SizeInFile = 24;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        FunctionNameCrc = stream.ReadUInt32();
        JustAStub = stream.ReadUInt32();
        ObjectIndex = stream.ReadInt32();
        NumExpressionParams = stream.ReadInt32();
        ExpressionParamsOffset = stream.ReadUInt32();
        stream.Skip(4);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxExpression. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
        
        //Read expression params
        long posBeforeReadingParams = stream.Position;
        if (NumExpressionParams > 0)
        {
            stream.Seek(ExpressionParamsOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumExpressionParams; i++)
            {
                VfxExpressionParam vfxExpressionParam = new();
                vfxExpressionParam.Read(stream);
                Parameters.Add(vfxExpressionParam);
            }
        }
        stream.Seek(posBeforeReadingParams, SeekOrigin.Begin); //Reset position so EffectFile can read VfxExpressions sequentially
    }
}