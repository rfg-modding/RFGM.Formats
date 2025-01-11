using RFGM.Formats.Streams;

namespace RFGM.Formats.Asset;

//Version 5 of the .asm_pc files used in Red Faction Guerrilla.
//asm = Asset Assembler. They are used to define what files make up an asset, what files are held by str2_pc files,
//and contain metadata also found in packfile headers like the data offset and size of files in the str2_pc files they reference.
//Also note that in the context of RFG a container is a str2_pc file, and a primitive is a file inside that container or inside a vpp_pc file.
public class AsmFile(string name)
{
    public string Name = name;
    public uint Signature;

    public ushort Version;

    //public ushort ContainerCount; //Found in asm_pc files
    public List<AsmContainer> Containers = new();

    public const uint ExpectedSignature = 3203399405;
    public const ushort ExpectedVersion = 5;

    public bool Read(Stream stream, out string error)
    {
        error = string.Empty;

        Signature = stream.ReadUInt32();
        Version = stream.ReadUInt16();
        if (Signature != ExpectedSignature)
        {
            error = $"Invalid .asm_pc file signature. Expected {ExpectedSignature}, found {Signature}.";
            return false;
        }
        if (Version != ExpectedVersion) //Only have seen and reversed version 36
        {
            error = $"Invalid .asm_pc file version. Expected {ExpectedVersion}, found {Version}.";
            return false;
        }

        ushort numContainers = stream.ReadUInt16();
        for (int i = 0; i < numContainers; i++)
        {
            AsmContainer container = new();
            if (!container.Read(stream, out string containerError))
            {
                error = $"Error reading .asm_pc file container '{container}'.{Environment.NewLine} - {containerError}";
                return false;
            }

            Containers.Add(container);
        }

        return true;
    }

    public bool Write(Stream stream, out string error)
    {
        error = string.Empty;

        if (Containers.Count > ushort.MaxValue)
        {
            error = $"Exceeded max container count of {ushort.MaxValue}.";
            return false;
        }

        stream.WriteUInt32(ExpectedSignature);
        stream.WriteUInt16(Version);
        stream.WriteUInt16((ushort)Containers.Count);
        foreach (AsmContainer container in Containers)
        {
            if (!container.Write(stream, out string containerError))
            {
                error = $"Error writing .asm_pc file container '{container.Name}'.{Environment.NewLine} - {containerError}";
                return false;
            }
        }

        stream.Flush();
        return true;
    }
}