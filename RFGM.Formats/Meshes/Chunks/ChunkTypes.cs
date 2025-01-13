using System.Numerics;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes.Chunks;

public struct Destroyable()
{
    public DestroyableHeader Header;

    public List<Subpiece> Subpieces = new();
    public List<SubpieceData> SubpieceData = new();
    public List<Link> Links = new();
    public List<Dlod> Dlods = new();
    
    //TODO: Fix
    //Note: These aren't read yet. Chunk format hasn't been 100% reversed yet.
    public List<RbbNode> RbbNodes = new();
    public DestroyableInstanceData InstanceData = new();
    
    //Additional data stored in a separate part of the chunk file
    public uint UID = uint.MaxValue;
    public string Name = string.Empty;
    public uint IsDestroyable = 0;
    public uint NumSnapPoints = 0;
    public ChunkSnapPoint[] SnapPoints = new ChunkSnapPoint[10];
}

public struct DestroyableHeader
{
    public uint AabbTreeOffset; //rfg_rbb_node offset
    public uint ObjectsOffset; //rfg_subpiece_base offset
    public uint ExtraDataOffset; //rfg_subpiece_base_extra_data offset
    public int NumObjects;
    public uint BaseLinksOffset; //rfg_links_base offset
    public int NumLinks;
    public uint DlodsOffset; //rfg_dlod_base offset
    public int NumDlods;
    public uint InstanceDataOffset; //rfg_destroyable_base_instance_data offset
    public uint TransformBufferOffset; //unsigned char buffer offset
    public float Mass;
    
    private const long SizeInFile = 44;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        AabbTreeOffset = stream.ReadUInt32();
        ObjectsOffset = stream.ReadUInt32();
        ExtraDataOffset = stream.ReadUInt32();
        NumObjects = stream.ReadInt32();
        BaseLinksOffset = stream.ReadUInt32();
        NumLinks = stream.ReadInt32();
        DlodsOffset = stream.ReadUInt32();
        NumDlods = stream.ReadInt32();
        InstanceDataOffset = stream.ReadUInt32();
        TransformBufferOffset = stream.ReadUInt32();
        Mass = stream.ReadFloat();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for DestroyableHeader. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct ChunkSnapPoint
{
    public Matrix3x3 Orient;
    public Vector3 Position;
    
    private const long SizeInFile = 48;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        Orient = stream.ReadStruct<Matrix3x3>();
        Position = stream.ReadStruct<Vector3>();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for ChunkSnapPoint. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct Subpiece
{
    public Vector3 Bmin;
    public Vector3 Bmax;
    public Vector3 Position;
    public Vector3 CenterOfMass;
    public float Mass;
    public uint DlodKey;
    public uint LinksOffset; //ushort offset
    public byte PhysicalMaterialIndex;
    public byte ShapeType;
    public byte NumLinks;
    public byte Flags;
    
    private const long SizeInFile = 64;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        Bmin = stream.ReadStruct<Vector3>();
        Bmax = stream.ReadStruct<Vector3>();
        Position = stream.ReadStruct<Vector3>();
        CenterOfMass = stream.ReadStruct<Vector3>();
        Mass = stream.ReadFloat();
        DlodKey = stream.ReadUInt32();
        LinksOffset = stream.ReadUInt32();
        PhysicalMaterialIndex = stream.ReadUInt8();
        ShapeType = stream.ReadUInt8();
        NumLinks = stream.ReadUInt8();
        Flags = stream.ReadUInt8();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Subpiece. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct SubpieceData
{
    public uint ShapeOffset; //havok shape offset
    public ushort CollisionModel;
    public ushort RenderSubpiece;
    public uint Unknown0;
    
    private const long SizeInFile = 12;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        ShapeOffset = stream.ReadUInt32();
        CollisionModel = stream.ReadUInt16();
        RenderSubpiece = stream.ReadUInt16();
        Unknown0 = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for SubpieceData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct Link()
{
    public int YieldMax = 0;
    public float Area = 0;
    public short[] Obj = new short[2];
    public byte Flags = 0;

    private const long SizeInFile = 16;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        YieldMax = stream.ReadInt32();
        Area = stream.ReadFloat();
        Obj[0] = stream.ReadInt16();
        Obj[1] = stream.ReadInt16();
        Flags = stream.ReadUInt8();
        stream.Skip(3);
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Link. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct Dlod
{
    public uint NameHash;
    public Vector3 Pos;
    public Matrix3x3 Orient;
    public ushort RenderSubpiece;
    public ushort FirstPiece;
    public byte MaxPieces;
    
    private const long SizeInFile = 60;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        NameHash = stream.ReadUInt32();
        Pos = stream.ReadStruct<Vector3>();
        Orient = stream.ReadStruct<Matrix3x3>();
        RenderSubpiece = stream.ReadUInt16();
        FirstPiece = stream.ReadUInt16();
        MaxPieces = stream.ReadUInt8();
        stream.Skip(3);
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for Dlod. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct RbbAabb
{
    public short MinX;
    public short MinY;
    public short MinZ;
    public short MaxX;
    public short MaxY;
    public short MaxZ;
    
    private const long SizeInFile = 12;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif

        MinX = stream.ReadInt16();
        MinY = stream.ReadInt16();
        MinZ = stream.ReadInt16();
        MaxX = stream.ReadInt16();
        MaxY = stream.ReadInt16();
        MaxZ = stream.ReadInt16();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RbbAabb. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct RbbNode()
{
    public int NumObjects = 0;
    public RbbAabb Aabb = new();
    public uint NodeDataOffset = 0; //et_ptr_offset<unsigned char, 0> node_data;
    
    private const long SizeInFile = 20;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        NumObjects = stream.ReadInt32();
        Aabb.Read(stream);
        NodeDataOffset = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for RbbNode. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}

public struct DestroyableInstanceData
{
    public uint ObjectsOffset;
    public uint LinksOffset;
    public uint DlodsOffset;
    public uint DataSize;
    public uint BufferOffset; //et_ptr_offset<unsigned char, 0> buffer;
    
    private const long SizeInFile = 20;

    public void Read(Stream stream)
    {
#if DEBUG
        long startPos = stream.Position;        
#endif
        
        ObjectsOffset = stream.ReadUInt32();
        LinksOffset = stream.ReadUInt32();
        DlodsOffset = stream.ReadUInt32();
        DataSize = stream.ReadUInt32();
        BufferOffset = stream.ReadUInt32();
        
#if DEBUG
        long endPos = stream.Position;
        long bytesRead = endPos - startPos;
        if (bytesRead != SizeInFile)
        {
            throw new Exception($"Invalid size for DestroyableInstanceData. Expected {SizeInFile} bytes, read {bytesRead} bytes");
        }
#endif
    }
}