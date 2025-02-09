using System.Runtime.InteropServices;
using RFGM.Formats.Materials;
using RFGM.Formats.Meshes.Chunks;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

//Chunk mesh. Buildings and other destructible objects are stored in this format. Extension = cchk_pc|gchk_pc
public class ChunkFile(string name)
{
    public string Name { get; private set; }= name;
    public bool LoadedCpuFile { get; private set; } = false;

    public ChunkFileHeader Header;

    public MeshConfig Config = new();
    public List<(string, long)> Textures = new();
    public List<RfgMaterial> Materials = new();
    public List<GeneralObject> GeneralObjects = new();
    public List<Destroyable> Destroyables = new();
    public List<ScriptPoint> ScriptPoints = new();
    public List<ObjectIdentifier> Identifiers = new();
    public List<ObjectSkirt> ObjectSkirts = new();
    public List<LightClipObject> LightClipObjects = new();

    private const uint HavokSignature = 1212891981;

    public void ReadHeader(Stream cpuFile)
    {
        //Validate header
        Header.Read(cpuFile);
        if (Header.Signature != 0xB0CEEFA5)
        {
            throw new Exception($"Invalid chunk file signature. Expected 2966351781, found {Header.Signature}.");
        }

        if (Header.Version != 56)
        {
            throw new Exception($"Invalid chunk file version. Expected 56, found {Header.Version}.");
        }

        if (Header.SourceVersion != 20)
        {
            throw new Exception($"Invalid chunk file source version. Expected 20, found {Header.SourceVersion}.");
        }

        //After reading the 656 header bytes the game is hardcoded to jump to offset 704 and reader the mesh config
        if (Header.RenderCpuDataOffset != 704)
        {
            throw new Exception($"Unexpected render cpu data offset. Expected 704, found {Header.RenderCpuDataOffset}.");
        }
        cpuFile.Seek(704, SeekOrigin.Begin);
        Config.Read(cpuFile);

        //Read texture names
        cpuFile.AlignRead(16);
        uint textureNamesBlockSize = cpuFile.ReadUInt32();
        long textureNamesStart = cpuFile.Position;
        Textures = cpuFile.ReadSizedStringListWithOffsets(textureNamesBlockSize);
        
        cpuFile.AlignRead(16);
        uint materialMapOffsetData = cpuFile.ReadUInt32(); //Likely only set to a valid value at runtime
        uint numMaterials = cpuFile.ReadUInt32();
        cpuFile.Skip(numMaterials * 4); //Potentially a list of material IDs or offsets //TODO: Read this data and figure out what it is
        cpuFile.AlignRead(16);
        cpuFile.Skip(numMaterials * 8); //TODO: Figure out what this data is

        if (numMaterials > 0)
        {
            cpuFile.AlignRead(16);
            for (int i = 0; i < numMaterials; i++)
            {
                cpuFile.AlignRead(16);
                RfgMaterial material = new();
                material.Read(cpuFile);
                Materials.Add(material);
            }
        }
        cpuFile.AlignRead(64);
        
        //TODO: Apply this change to the other mesh types and read all the instances to make sure it works correctly
        //TODO: Do same checks on materialMapIndex and TextureDesc.Name that we're doing on the chunk files
        //Set texture names on the TextureDesc objects based on their offsets
        foreach ((string textureName, long offset) in Textures)
        {
            foreach (RfgMaterial material in Materials)
            {
                for (var i = 0; i < material.Textures.Count; i++)
                {
                    TextureDesc textureDesc = material.Textures[i];
                    if (textureDesc.NameOffset == offset)
                    {
                        material.Textures[i] = textureDesc with { Name = textureName };
                    }
                }
            }
        }
        
        if (Header.NumGeneralObjects > 0)
        {
            for (int i = 0; i < Header.NumGeneralObjects; i++)
            {
                GeneralObject obj = new();
                obj.Read(cpuFile);
                GeneralObjects.Add(obj);
            }

            foreach (GeneralObject obj in GeneralObjects)
            {
                string name = cpuFile.ReadAsciiNullTerminatedString();
                obj.Name = name;
            }
        }

        //TODO: Figure out what data is here. Appears to be some kind of tree
        //TODO: Once that's done add check make sure cpuFile.Position == Header.DestructionOffset

        //Skip to destroyables. Haven't fully reversed the format yet
        cpuFile.Seek(Header.DestructionOffset, SeekOrigin.Begin);
        cpuFile.AlignRead(128);
        
        long destroyablesStartPos = cpuFile.Position;
        byte[] destroyablesData = new byte[Header.DestructionDataSize];
        cpuFile.ReadExactly(destroyablesData);
        cpuFile.Seek(destroyablesStartPos, SeekOrigin.Begin);

        int numDestroyables = cpuFile.ReadInt32();
        cpuFile.Skip((numDestroyables * 8) + 4); //TODO: Is there any important data here?

        //Read destroyables
        for (int i = 0; i < numDestroyables; i++)
        {
            cpuFile.AlignRead(16);
            Destroyable destroyable = new();
            destroyable.Header.Read(cpuFile);
            cpuFile.AlignRead(128);

            for (int j = 0; j < destroyable.Header.NumObjects; j++)
            {
                Subpiece subpiece = new();
                subpiece.Read(cpuFile);
                destroyable.Subpieces.Add(subpiece);
            }

            for (int j = 0; j < destroyable.Header.NumObjects; j++)
            {
                SubpieceData subpieceData = new();
                subpieceData.Read(cpuFile);
                destroyable.SubpieceData.Add(subpieceData);
            }

            foreach (Subpiece subpiece in destroyable.Subpieces)
            {
                for (int j = 0; j < subpiece.NumLinks; j++)
                {
                    subpiece.Links.Add(cpuFile.ReadUInt16());
                }
            }
            cpuFile.AlignRead(4);

            //Read links
            for (int j = 0; j < destroyable.Header.NumLinks; j++)
            {
                Link link = new();
                link.Read(cpuFile);
                destroyable.Links.Add(link);
            }
            cpuFile.AlignRead(4);

            //Read dlods
            for (int j = 0; j < destroyable.Header.NumDlods; j++)
            {
                Dlod dlod = new();
                dlod.Read(cpuFile);
                destroyable.Dlods.Add(dlod);
            }
            cpuFile.AlignRead(4);

            //Read rbb node tree
            ReadRbbNodesFromByteArray(destroyablesData, destroyablesStartPos, cpuFile.Position, out long newStreamPos, out destroyable.RbbNodes);
            cpuFile.Position = newStreamPos;
            cpuFile.AlignRead(4);

            destroyable.InstanceData.Read(cpuFile);
            cpuFile.AlignRead(16);

            //TODO: Save this data on the destroyable object
            uint maybeTransformBufferSize = cpuFile.Peek<uint>(); //Includes the size of this uint
            byte[] maybeTransformBuffer = new byte[maybeTransformBufferSize];
            cpuFile.ReadExactly(maybeTransformBuffer);

            cpuFile.AlignRead(16);
            Destroyables.Add(destroyable);
        }

        cpuFile.AlignRead(64);
        if (cpuFile.Position != Header.CollisionModelDataOffset)
        {
            throw new Exception($"Did not read all destroyable data. Expected offset to be {Header.CollisionModelDataOffset} after reading destroyables. Currently is {cpuFile.Position}");
        }

        //Skip collision models. Some kind of havok data format that hasn't been reversed yet.
        if (!SkipHavokData(cpuFile))
        {
            throw new Exception("Failed to skip havok data in chunk file.");
        }
        cpuFile.AlignRead(64);

        for (int i = 0; i < Header.NumScriptPoints; i++)
        {
            ScriptPoint scriptPoint = new();
            scriptPoint.Read(cpuFile);
            ScriptPoints.Add(scriptPoint);
        }
        cpuFile.AlignRead(64);

        for (int i = 0; i < Header.NumObjectIdentifiers; i++)
        {
            ObjectIdentifier identifier = new();
            identifier.Read(cpuFile);
            Identifiers.Add(identifier);
        }
        cpuFile.AlignRead(64);

        //TODO: Figure out if NumObjectSkirts and NumLightClipObjects need to be calculated or set based on other data in the .cchk_pc file
        //TODO: After reading every single vanilla cchk file none of them have object skirts or light clips (at least based on the count in the file header)
        for (int i = 0; i < Header.NumObjectSkirts; i++)
        {
            ObjectSkirt skirt = new();
            skirt.Read(cpuFile);
            ObjectSkirts.Add(skirt);
        }
        if (Header.NumObjectSkirts > 0)
        {
            foreach (ObjectSkirt skirt in ObjectSkirts)
            {
                for (int i = 0; i < skirt.NumWeldEdges; i++)
                {
                    ObjectSkirtEdge edge = new();
                    edge.Read(cpuFile);
                    skirt.Edges.Add(edge);
                }
            }
        }
        cpuFile.AlignRead(64);

        for (int i = 0; i < Header.NumLightClipObjects; i++)
        {
            LightClipObject lightClipObject = new();
            lightClipObject.Read(cpuFile);
            LightClipObjects.Add(lightClipObject);
        }
        if (Header.NumLightClipObjects > 0)
        {
            foreach (LightClipObject lightClipObject in LightClipObjects)
            {
                string name = cpuFile.ReadAsciiNullTerminatedString();
                lightClipObject.Name = name;
            }
        }
        cpuFile.AlignRead(64);

        if (Header.Version >= 53)
        {
            //TODO: The decompilation for chunk_base_load_internal indicates that there should be "destroyable kd trees" here. It's not read yet since we don't need it for meshes
            //TODO: Add code to read them
        }

        LoadedCpuFile = true;
    }

    private unsafe void ReadRbbNodesFromByteArray(byte[] destroyablesData, long destroyablesStartPos, long initialStreamPos, out long newStreamPos, out List<RbbNode> rbbNodes)
    {
        GCHandle? handle = null;
        try
        {
            //TODO: Add code to fill rbbNodes list
            //TODO: Add code to read the data that lies between/after some nodes
            //TODO: ^^Figure out what that data is.
            //TODO: Rewrite this code in safe C# in a way similar to the rest of the file format code (using streams)
            handle = GCHandle.Alloc(destroyablesData, GCHandleType.Pinned);
            rbbNodes = new List<RbbNode>();
            int streamDistanceFromDestroyableStart = (int)(initialStreamPos - destroyablesStartPos);

            fixed (byte* data = destroyablesData)
            {
                RbbNode* node = (RbbNode*)(data + streamDistanceFromDestroyableStart);
                streamDistanceFromDestroyableStart += 20;
                ReadRbbNodesRecursive(node, data, &streamDistanceFromDestroyableStart);
            }

            newStreamPos = destroyablesStartPos + streamDistanceFromDestroyableStart;
        }
        finally
        {
            if (handle is { IsAllocated: true })
            {
                handle.Value.Free();
            }
        }
    }

    private unsafe void ReadRbbNodesRecursive(RbbNode* node, byte* destroyableStart, int* streamDistanceFromDestroyableStart)
    {
        if (node->NumObjects < 0)
            return;
        
        while (node->NumObjects == 0)
        {
            int offsetFieldDistanceFromDestroyableStart = (int)((long)destroyableStart - (long)&node->NodeDataOffset);
            node->NodeDataOffset = offsetFieldDistanceFromDestroyableStart + *streamDistanceFromDestroyableStart;
            *streamDistanceFromDestroyableStart += 40;

            RbbNode* childNode = (RbbNode*)((byte*)&node->NodeDataOffset + node->NodeDataOffset);
            ReadRbbNodesRecursive(childNode, destroyableStart, streamDistanceFromDestroyableStart);

            RbbNode* nextNode = (RbbNode*)((byte*)&node->NodeDataOffset + node->NodeDataOffset + 20);
            if (nextNode->NumObjects < 0)
                return;

            node = nextNode;
        }

        node->NodeDataOffset = (int)((long)destroyableStart - (long)&node->NodeDataOffset) + *streamDistanceFromDestroyableStart;
        *streamDistanceFromDestroyableStart += node->NumObjects * 4; //Seek past object data which follows some nodes
    }
    
    private bool SkipHavokData(Stream stream)
    {
        long startPos = stream.Position;
        uint maybeSignature = stream.ReadUInt32();
        if (maybeSignature != HavokSignature)
            return false;

        stream.Skip(4);
        uint size = stream.ReadUInt32();
        stream.Seek(startPos + size, SeekOrigin.Begin);
        return true;
    }

    public MeshInstanceData ReadData(Stream gpuFile)
    {
        if (!LoadedCpuFile)
        {
            throw new Exception("You must call ChunkFile.ReadHeader() before calling ChunkFile.ReadData() on ChunkFile");
        }

        //Read index buffer
        gpuFile.Seek(16, SeekOrigin.Begin);
        byte[] indices = new byte[Config.NumIndices * Config.IndexSize];
        gpuFile.ReadExactly(indices);

        //Read vertex buffer
        gpuFile.AlignRead(16);
        byte[] vertices = new byte[Config.NumVertices * Config.VertexStride0];
        gpuFile.ReadExactly(vertices);

        return new MeshInstanceData(Config, vertices, indices);
    }
}