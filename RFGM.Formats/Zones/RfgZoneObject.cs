using System.Numerics;
using System.Runtime.CompilerServices;
using RFGM.Formats.Hashes;
using RFGM.Formats.Math;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Zones;

public class RfgZoneObject
{
    public uint ClassnameHash { get; private set; }
    public uint Handle { get; private set; }
    public BoundingBox BBox { get; private set; }
    public ushort Flags { get; private set; }
    public ushort BlockSize { get; private set; }
    public uint Parent { get; private set; }
    public uint Sibling { get; private set; }
    public uint Child { get; private set; }
    public uint Num { get; private set; }
    public ushort NumProperties { get; private set; }
    public ushort PropertyBlockSize { get; private set; }

    public IReadOnlyList<RfgZoneObjectProperty> Properties { get; private set; } = Array.Empty<RfgZoneObjectProperty>();
    
    public string Classname => HashDictionary.FindOriginString(ClassnameHash) ?? "Unknown";
    public const uint InvalidHandle = uint.MaxValue;

    public bool Load(Stream stream, out string error)
    {
        error = string.Empty;
        try
        {
#if DEBUG
            long startPos = stream.Position;
#endif
            //Read object header
            ClassnameHash = stream.ReadUInt32();
            Handle = stream.ReadUInt32();
            BBox = stream.ReadStruct<BoundingBox>();
            Flags = stream.ReadUInt16();
            BlockSize = stream.ReadUInt16();
            Parent = stream.ReadUInt32();
            Sibling = stream.ReadUInt32();
            Child = stream.ReadUInt32();
            Num = stream.ReadUInt32();
            NumProperties = stream.ReadUInt16();
            PropertyBlockSize = stream.ReadUInt16();

#if DEBUG
            if (stream.Position != startPos + 56)
            {
                error = $"Expected to read 56 bytes for object header. Actually read {stream.Position - startPos}. Handle: {Handle}, Num: {Num}";
                return false;
            }
            
            long propertiesStart = stream.Position;
#endif

            //Read object properties
            List<RfgZoneObjectProperty> properties = new();
            for (int i = 0; i < NumProperties; i++)
            {
                ushort type = stream.ReadUInt16();
                ushort size = stream.ReadUInt16();
                uint nameHash = stream.ReadUInt32();
                byte[] data = stream.ReadBytes(size);
                RfgZoneObjectProperty prop = new(type, size, nameHash, data); //TODO: Look into using StreamView on the source stream instead of making a copy here
                properties.Add(prop);
                stream.AlignRead(4);
            }
            Properties = properties;
            
#if DEBUG
            long propertiesEnd = stream.Position;
            if (propertiesEnd != propertiesStart + PropertyBlockSize)
            {
                error = $"Expected to read {PropertyBlockSize} bytes for object properties. Actually read {stream.Position - startPos}. Handle: {Handle}, Num: {Num}";
                return false;
            }
#endif

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private RfgZoneObjectProperty? GetProperty(string name)
    {
        uint nameHash = Hash.HashVolitionCRC(name, 0);
        return Properties.FirstOrDefault(property => property.NameHash == nameHash);
    }

    private unsafe T? GetPropertyTyped<T>(string name, ushort type) where T : unmanaged
    {
        RfgZoneObjectProperty? property = GetProperty(name);
        if (property is null)
            return null;
        if (property.Type != type)
            throw new Exception($"Invalid type for zone object property '{name}'. Expected type {type}, found {property.Type}.");
        if (property.Size != Unsafe.SizeOf<T>())
            throw new Exception($"Invalid size for zone object property '{name}'. Expected {Unsafe.SizeOf<T>()}, found {property.Size}.");
        
        return *(T*)property.Data[0];
    }
    
    public float? GetFloat(string name)
    {
        return GetPropertyTyped<float>(name, 5);
    }
    
    public short? GetInt16(string name)
    {
        return GetPropertyTyped<short>(name, 5);
    }

    public int? GetInt32(string name)
    {
        return GetPropertyTyped<int>(name, 5);
    }

    public byte? GetUInt8(string name)
    {
        return GetPropertyTyped<byte>(name, 5);
    }
    
    public ushort? GetUInt16(string name)
    {
        return GetPropertyTyped<ushort>(name, 5);
    }
    
    public uint? GetUInt32(string name)
    {
        return GetPropertyTyped<uint>(name, 5);
    }

    public bool? GetBool(string name)
    {
        return GetPropertyTyped<bool>(name, 5);
    }

    public Vector3? GetVector3(string name)
    {
        return GetPropertyTyped<Vector3>(name, 5);
    }

    public Matrix3x3? GetMat3(string name)
    {
        return GetPropertyTyped<Matrix3x3>(name, 5);
    }

    public PositionOrient? GetPositionOrient(string name)
    {
        return GetPropertyTyped<PositionOrient>(name, 5);
    }

    public BoundingBox? GetBoundingBox(string name)
    {
        return GetPropertyTyped<BoundingBox>(name, 5);
    }

    public byte[] GetBuffer(string name)
    {
        RfgZoneObjectProperty? property = GetProperty(name);
        if (property is null)
            return [];
        if (property.Type != 6)
            throw new Exception($"Invalid type for zone object buffer property '{name}'. Expected type 6, found {property.Type}.");
        
        return property.Data;
    }

    public string? GetString(string name)
    {
        RfgZoneObjectProperty? property = GetProperty(name);
        if (property is null)
            return null;
        if (property.Type != 4)
            throw new Exception($"Invalid type for zone object string property '{name}'. Expected type 4, found {property.Type}.");
        
        string result = System.Text.Encoding.ASCII.GetString(property.Data);
        if (result.EndsWith('\0'))
        {
            result = result.Substring(0, result.Length - 1); //Don't include null terminator
        }                
        return result;
    }

    //TODO: Implement the ConstraintTemplate struct and return that here
    public byte[] GetConstraintTemplate(string name)
    {
        return GetBuffer(name);
    }
}