using RFGM.Formats.Streams;

namespace RFGM.Formats.Asset;

public class AsmContainer
{
    public string Name = string.Empty;
    public ContainerType Type;

    public ContainerFlags Flags;

    //public ushort PrimitiveCount; //Found the asm_pc files but not duplicated here since Primitives has .Count
    public uint DataOffset; //TODO: Does overflow need to be accounted for here in large vpps like terr01_l0?

    //public uint SizeCount; //Found in the asm_pc files
    public uint CompressedSize;

    public List<AsmPrimitive> Primitives = new();
    //public List<int> PrimitiveSizes = new();

    public bool Read(Stream stream, out string error)
    {
        error = string.Empty;

        var nameLength = stream.ReadUInt16();
        Name = stream.ReadAsciiString(nameLength);
        Type = (ContainerType)stream.ReadUInt8();
        Flags = (ContainerFlags)stream.ReadUInt16();
        var numPrimitives = stream.ReadUInt16();
        DataOffset = stream.ReadUInt32();
        var sizeCount = stream.ReadUInt32();
        CompressedSize = stream.ReadUInt32();

        List<int> primitiveSizes = new();
        for (var i = 0; i < sizeCount; i++)
        {
            primitiveSizes.Add(stream.ReadInt32());
        }

        for (var i = 0; i < numPrimitives; i++)
        {
            AsmPrimitive primitive = new();
            if (!primitive.Read(stream, out var primitiveError))
            {
                error = $"Failed to read .asm_pc file primitive '{Name}'.{Environment.NewLine}- {primitiveError}";
            }
            
            Primitives.Add(primitive);
        }

        //Make sure sizeCount matches the number of sizes on the actual primitives
        var calculatedSizeCount = 0;
        List<int> calculatedSizes = new();
        foreach (var primitive in Primitives)
        {
            //Calculate the number of sizes to make sure everything matches up. HeaderSize & DataSize correspond to the cpu file and gpu file (e.g. .cpeg_pc and .gpeg_pc).
            //Some formats only can consist of only a cpu file (e.g. .cefct_pc). In that case DataSize will be 0 and it's not counted.
            if (primitive.HeaderSize != 0)
            {
                calculatedSizes.Add(primitive.HeaderSize);
                calculatedSizeCount++;
            }
            if (primitive.DataSize != 0)
            {
                calculatedSizes.Add(primitive.DataSize);
                calculatedSizeCount++;
            }
        }

        if (Flags.HasFlag(ContainerFlags.HasSizeList) && calculatedSizeCount != primitiveSizes.Count)
        {
            var firstDifferentIndex = -1;
            for (var i = 0; i < System.Math.Min(primitiveSizes.Count, calculatedSizes.Count); i++)
            {
                if (primitiveSizes[i] != calculatedSizes[i])
                {
                    firstDifferentIndex = i;
                    break;
                }
            }

            var asmFileHasExtraSizes = primitiveSizes.Count > calculatedSizes.Count && firstDifferentIndex == -1;
            if (!asmFileHasExtraSizes) //Handle modded asm_pc files that mistakenly have extra sizes. E.g. 15.vpp_pc/15.asm_pc in the current version of Terraform patch at the time of writing. Safe to ignore in that case.
            {
                error = $"Primitive size count mismatch. Expected {primitiveSizes.Count}, found {calculatedSizeCount}.";
                return false;   
            }
        }

        return true;
    }

    public bool Write(Stream stream, out string error)
    {
        error = string.Empty;
        if (Name.Length > ushort.MaxValue)
        {
            error = $"Exceeded max container name length of {ushort.MaxValue}.";
            return false;
        }
        if (Primitives.Count > ushort.MaxValue)
        {
            error = $"Exceeded max primitive count of {ushort.MaxValue}.";
            return false;
        }
        
        List<int> primitiveSizes = new();
        if (Flags.HasFlag(ContainerFlags.HasSizeList))
        {
            foreach (var primitive in Primitives)
            {
                if (primitive.HeaderSize != 0)
                    primitiveSizes.Add(primitive.HeaderSize);
                if (primitive.DataSize != 0)
                    primitiveSizes.Add(primitive.DataSize);
            }   
        }

        stream.WriteUInt16((ushort)Name.Length);
        stream.WriteAsciiString(Name);
        stream.WriteUInt8((byte)Type);
        stream.WriteUInt16((ushort)Flags);
        stream.WriteUInt16((ushort)Primitives.Count);
        stream.WriteUInt32(DataOffset);
        stream.WriteUInt32((uint)primitiveSizes.Count);
        stream.WriteUInt32(CompressedSize);

        foreach (var size in primitiveSizes)
        {
            stream.WriteInt32(size);
        }

        foreach (var primitive in Primitives)
        {
            if (!primitive.Write(stream, out var primitiveError))
            {
                error = $"Error writing .asm_pc file primitive '{primitive.Name}'.{Environment.NewLine} - {primitiveError}";
                return false;
            }
        }

        return true;
    }
}