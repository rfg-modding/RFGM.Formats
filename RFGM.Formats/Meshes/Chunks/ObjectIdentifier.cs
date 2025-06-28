using System.Diagnostics;
using System.Runtime.InteropServices;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public class ObjectIdentifier
{
    public uint UniqueId;
    public string Name = "";
    public int ObjectIndex;
    public uint IsDestroyable; //TODO: Figure out if we can represent this as a C# bool, then convert back to uint on write
    public uint NumSnapPoints;
    public List<Matrix4x3> SnapPoints = new();
    
    private const long SizeInFile = 520;
    
    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        UniqueId = stream.ReadUInt32();
        Name = stream.ReadSizedString(24);
        ObjectIndex = stream.ReadInt32();
        IsDestroyable = stream.ReadUInt32();
        NumSnapPoints = stream.ReadUInt32();

        Debug.Assert(Marshal.SizeOf<Matrix4x3>() == 48);
        long snapPointsEnd = stream.Position + (10 * Marshal.SizeOf<Matrix4x3>());
        for (int i = 0; i < NumSnapPoints; i++)
        {
            Matrix4x3 snapPoint = stream.ReadStruct<Matrix4x3>();
            SnapPoints.Add(snapPoint);
        }
        stream.Seek(snapPointsEnd, SeekOrigin.Begin); //Space is always reserved for 10 snap point matrices even if it doesn't have 10
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ObjectIdentifier. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}