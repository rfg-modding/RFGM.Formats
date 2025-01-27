using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
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
            var startPos = stream.Position;
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
            
            var propertiesStart = stream.Position;
#endif

            //Read object properties
            List<RfgZoneObjectProperty> properties = new();
            for (var i = 0; i < NumProperties; i++)
            {
                var type = stream.ReadUInt16();
                var size = stream.ReadUInt16();
                var nameHash = stream.ReadUInt32();
                var data = stream.ReadBytes(size);
                RfgZoneObjectProperty prop = new(type, size, nameHash, data); //TODO: Look into using StreamView on the source stream instead of making a copy here
                properties.Add(prop);
                stream.AlignRead(4);
            }
            Properties = properties;
            
#if DEBUG
            var propertiesEnd = stream.Position;
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
        var nameHash = Hash.HashVolitionCRC(name, 0);
        return Properties.FirstOrDefault(property => property.NameHash == nameHash);
    }

    private unsafe bool GetPropertyTyped<T>(string name, ushort type, out T? value) where T : unmanaged
    {
        value = null;

        var property = GetProperty(name);
        if (property is null)
            return false;
        if (property.Type != type)
            throw new Exception($"Invalid type for zone object property '{name}'. Expected type {type}, found {property.Type}.");
        if (property.Size != Unsafe.SizeOf<T>())
            throw new Exception($"Invalid size for zone object property '{name}'. Expected {Unsafe.SizeOf<T>()}, found {property.Size}.");

        fixed (byte* ptr = property.Data)
        {
            value = *(T*)ptr;
        }
        return true;
    }
    
    public bool GetFloat(string name, out float value)
    {
        if (GetPropertyTyped(name, 5, out float? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = 0;
        return false;
    }
    
    public bool GetInt16(string name, out short value)
    {
        if (GetPropertyTyped(name, 5, out short? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = 0;
        return false;
    }

    public bool GetInt32(string name, out int value)
    {
        if (GetPropertyTyped(name, 5, out int? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = 0;
        return false;
    }

    public bool GetUInt8(string name, out byte value)
    {
        if (GetPropertyTyped(name, 5, out byte? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = 0;
        return false;
    }
    
    public bool GetUInt16(string name, out ushort value)
    {
        if (GetPropertyTyped(name, 5, out ushort? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = 0;
        return false;
    }
    
    public bool GetUInt32(string name, out uint value)
    {
        if (GetPropertyTyped(name, 5, out uint? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = 0;
        return false;
    }

    public bool GetBool(string name, out bool value)
    {
        if (GetPropertyTyped(name, 5, out bool? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = false;
        return false;
    }

    public bool GetVec3(string name, out Vector3 value)
    {
        if (GetPropertyTyped(name, 5, out Vector3? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = Vector3.Zero;
        return false;
    }

    public bool GetMat3(string name, out Matrix3x3 value)
    {
        if (GetPropertyTyped(name, 5, out Matrix3x3? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = Matrix3x3.Identity;
        return false;
    }

    public bool GetPositionOrient(string name, out PositionOrient value)
    {
        if (GetPropertyTyped(name, 5, out PositionOrient? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = new PositionOrient
        {
            Position = Vector3.Zero,
            Orient = Matrix3x3.Identity,
        };
        return false;
    }

    public bool GetBBox(string name, out BoundingBox value)
    {
        if (GetPropertyTyped(name, 5, out BoundingBox? prop))
        {
            value = prop!.Value;
            return true;
        }

        value = new BoundingBox
        {
            Min = Vector3.Zero,
            Max = Vector3.Zero,
        };
        return false;
    }

    public bool GetBuffer(string name, out byte[] value)
    {
        var property = GetProperty(name);
        if (property is null)
        {
            value = [];
            return false;
        }
        if (property.Type != 6)
        {
            throw new Exception($"Invalid type for zone object buffer property '{name}'. Expected type 6, found {property.Type}.");
        }
        
        value = property.Data;
        return true;
    }

    public bool GetString(string name, out string value)
    {
        var property = GetProperty(name);
        if (property is null)
        {
            value = string.Empty;
            return false;
        }
        if (property.Type != 4)
        {
            throw new Exception($"Invalid type for zone object string property '{name}'. Expected type 4, found {property.Type}.");
        }
        
        var result = Encoding.ASCII.GetString(property.Data);
        if (result.EndsWith('\0'))
        {
            result = result.Substring(0, result.Length - 1); //Don't include null terminator
        }
        value = result;
        return true;
    }

    //TODO: Implement the ConstraintTemplate struct and return that here
    public bool GetConstraintTemplate(string name, out byte[] value)
    {
        var property = GetProperty(name);
        if (property is null)
        {
            value = [];
            return false;
        }
        if (property.Type != 5)
        {
            throw new Exception($"Invalid type for zone object constraint property '{name}'. Expected type 5, found {property.Type}.");
        }
        
        value = property.Data;
        return true;
    }
}