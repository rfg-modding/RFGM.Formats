using System.Runtime.InteropServices;
using RFGM.Formats.Meshes.Chunks;
using RFGM.Formats.Meshes.Shared;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Meshes;

//Chunk mesh. Buildings and other destructible objects are stored in this format. Extension = cchk_pc|gchk_pc
public class ChunkFile(string name)
{
    public string Name = name;
    public bool LoadedCpuFile { get; private set; } = false;

    public ChunkFileHeader Header;

    public MeshConfig Config = new();
    public List<string> Textures = new();
    List<RfgMaterial> Materials = new();
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
        Textures = cpuFile.ReadSizedStringList(textureNamesBlockSize);

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
                //TODO: Make sure we're not skipping any important data by doing this
                //cpuFile.Seek(MaterialOffsets[i], SeekOrigin.Begin);

                RfgMaterial material = new();
                material.Read(cpuFile);
                Materials.Add(material);
            }
        }
        cpuFile.AlignRead(64);
        
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

        //TODO: Remove once this BS is figured out
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

            //TODO: Port this to plain safe C#. Maybe keep it temporarily and run it on every file + check if this func + the rewrite have the same resulting file offset
            //Read rbb node tree
            ReadRbbNodesFromByteArray(destroyablesData, destroyablesStartPos, cpuFile.Position, out long newStreamPos, out destroyable.RbbNodes);
            cpuFile.Position = newStreamPos;
            cpuFile.AlignRead(4);

            destroyable.InstanceData.Read(cpuFile);
            cpuFile.AlignRead(16);

            //TODO: Save this data on the destroyable
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

    //C# reconstruction of the confusing rfg_rbb_read_nodes() function from rfg.exe. Used to help figure out how it works
    //This is temporary until its rewritten in safe C#
    private unsafe void ReadRbbNodesFromByteArray(byte[] destroyablesData, long destroyablesStartPos, long initialStreamPos, out long newStreamPos, out List<RbbNode> rbbNodes)
    {
        //TODO: Add code to convert from rfg_rbb_node to rbbNodes, read the node data list, and add to rbbNodes
        rbbNodes = new List<RbbNode>();
        int offset = (int)(initialStreamPos - destroyablesStartPos);
        GCHandle handle = GCHandle.Alloc(destroyablesData, GCHandleType.Pinned);

        fixed (byte* data = destroyablesData)
        {
            rfg_rbb_node* node = (rfg_rbb_node*)(data + offset);
            offset += 20;
            rfg_rbb_read_nodes(node, data, &offset);
        }

        newStreamPos = destroyablesStartPos + offset;
        handle.Free();
    }

    //TODO: Remove these temporary types once rbb node code is rewritten. These are temporarily here just to do pointer math exactly like the game
    private struct fp_aabb
    {
        public short min_x;
        public short min_y;
        public short min_z;
        public short max_x;
        public short max_y;
        public short max_z;
    }

    private struct et_ptr_offset<T> where T : unmanaged
    {
        public int m_offset;
    }

    private struct rfg_rbb_node()
    {
        public int num_objects = 0;

        public fp_aabb aabb = new();

        //public uint NodeDataOffset = 0; //et_ptr_offset<unsigned char, 0> node_data;
        public et_ptr_offset<byte> node_data;
    }

    //TODO: Figure out what the hell this is doing and convert it to safe C#
    private unsafe void rfg_rbb_read_nodes(rfg_rbb_node* node, byte* data, int* offset)
    {
        rfg_rbb_node* node_1 = node;
        int num_objects = node_1->num_objects;
        bool condition = (num_objects != 0);

        if (num_objects >= 0)
        {
            while (!condition)
            {
                //node_1->node_data.m_offset = *(uint*)((long)offset - (long)&node_1->node_data + (long)data);
                node_1->node_data.m_offset = (int)(*offset - (long)&node_1->node_data + (long)data);
                *offset += 40;

                rfg_rbb_read_nodes((rfg_rbb_node*)(node_1->node_data.m_offset + (byte*)&node_1->node_data), data, offset);
                int m_offset = node_1->node_data.m_offset;

                int nextNodeNumObjects = *(int*)(((byte*)&node_1->node_data) + m_offset + 20);

                node_1 = (rfg_rbb_node*)(((byte*)&node_1->node_data) + m_offset + 20);
                condition = (nextNodeNumObjects != 0);

                if (nextNodeNumObjects < 0)
                    return;
            }

            node_1->node_data.m_offset = (int)(*offset - (long)&node_1->node_data + (long)data);
            *offset += node_1->num_objects * 4;
        }
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