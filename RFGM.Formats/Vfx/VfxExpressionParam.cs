using System.Numerics;
using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Vfx;

public class VfxExpressionParam
{
    public bool Variable;
    public uint VariableCrc;
    public uint DataOffset;

    public string Name
    {
        get
        {
            if (VariableCrc == 0) 
                return "Unnamed";
            
            return HashDictionary.FindOriginString(VariableCrc) ?? "Unknown";
        }
    }

    public Vector4 Data = Vector4.Zero;
        
    private const long SizeInFile = 16;
    
    public void Read(Stream stream)
    {
#if DEBUG
        var startPos = stream.Position;        
#endif

        Variable = stream.ReadBoolean();
        stream.Skip(3);
        VariableCrc = stream.ReadUInt32();
        DataOffset = stream.ReadUInt32();
        stream.Skip(4);
        
#if DEBUG
        var endPos = stream.Position;
        var bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for VfxExpressionParam. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif

        //Notes about the vfx parameters in vanilla .cefct_pc files:
        //    - All vfx expressions have 6 parameters
        //    - The first parameter always has Variable = 1, a valid VariableCRC, and DataOffset = uint.MaxValue
        //    - The following parameters always have Variable = 0, VariableCRC = 0, and a valid DataOffset
        //    - The distance between DataOffset on all parameters is 16 (ignoring first one). So it can be safely assumed that all parameters data is 16 bytes long.
        //    - The exact meaning of the parameter data is unknown, but they appear to be floats. So for now they're represented as a Vec4.
        if (DataOffset != uint.MaxValue)
        {
            stream.Seek(DataOffset, SeekOrigin.Begin);
            Data = stream.ReadStruct<Vector4>();
            stream.Seek(endPos, SeekOrigin.Begin);   
        }

        //TODO: Figure out what the parameter data is for each type of parameter and expression function (e.g. data type, purpose, etc)
        //TODO: The easiest way is probably to look at how the game uses them. See the clerp_XXXX functions. E.g. clerp_scale_z()
    }
}