// This is a generated file! Please edit source .ksy file and use kaitai-struct-compiler to rebuild

using System.Collections.Generic;
using Kaitai;

namespace RFGM.Formats.Asm.Models
{

    /// <summary>
    /// ASM_PC is a format used by Volition games
    /// </summary>
    /// <remarks>
    /// Reference: TODO
    /// </remarks>
    public partial class RfgAsm : KaitaiStruct
    {
        public static RfgAsm FromFile(string fileName)
        {
            return new RfgAsm(new KaitaiStream(fileName));
        }

        public RfgAsm(KaitaiStream p__io, KaitaiStruct p__parent = null, RfgAsm p__root = null) : base(p__io)
        {
            m_parent = p__parent;
            m_root = p__root ?? this;
            _read();
        }
        private void _read()
        {
            _header = new HeaderBlock(m_io, this, m_root);
            _containers = new List<Container>();
            for (var i = 0; i < Header.NumContainers; i++)
            {
                _containers.Add(new Container(m_io, this, m_root));
            }
        }
        public partial class HeaderBlock : KaitaiStruct
        {
            public static HeaderBlock FromFile(string fileName)
            {
                return new HeaderBlock(new KaitaiStream(fileName));
            }

            public HeaderBlock(KaitaiStream p__io, RfgAsm p__parent = null, RfgAsm p__root = null) : base(p__io)
            {
                m_parent = p__parent;
                m_root = p__root;
                _read();
            }
            private void _read()
            {
                _magic = m_io.ReadBytes(4);
                if (!((KaitaiStream.ByteArrayCompare(Magic, new byte[] { 237, 254, 239, 190 }) == 0)))
                {
                    throw new ValidationNotEqualError(new byte[] { 237, 254, 239, 190 }, Magic, M_Io, "/types/header_block/seq/0");
                }
                _version = m_io.ReadBytes(2);
                if (!((KaitaiStream.ByteArrayCompare(Version, new byte[] { 5, 0 }) == 0)))
                {
                    throw new ValidationNotEqualError(new byte[] { 5, 0 }, Version, M_Io, "/types/header_block/seq/1");
                }
                _numContainers = m_io.ReadU2le();
            }
            private byte[] _magic;
            private byte[] _version;
            private ushort _numContainers;
            private RfgAsm m_root;
            private RfgAsm m_parent;
            public byte[] Magic { get { return _magic; } }
            public byte[] Version { get { return _version; } }
            public ushort NumContainers { get { return _numContainers; } }
            public RfgAsm M_Root { get { return m_root; } }
            public RfgAsm M_Parent { get { return m_parent; } }
        }
        public partial class Container : KaitaiStruct
        {
            public static Container FromFile(string fileName)
            {
                return new Container(new KaitaiStream(fileName));
            }


            public enum ContainerType
            {
                None = 0,
                Glass = 1,
                EffectsEnv = 2,
                EffectsPreload = 3,
                EffectsDlc = 4,
                MpEffects = 5,
                LayerSmall = 6,
                LayerLarge = 7,
                Audio = 8,
                ClothSim = 9,
                Decals = 10,
                DecalsPreload = 11,
                Fsm = 12,
                Ui = 13,
                Env = 14,
                Chunk = 15,
                ChunkPreload = 16,
                Stitch = 17,
                World = 18,
                HumanHead = 19,
                Human = 20,
                Player = 21,
                Items = 22,
                ItemsPreload = 23,
                ItemsMpPreload = 24,
                ItemsDlc = 25,
                WeaponLarge = 26,
                WeaponSmall = 27,
                Skybox = 28,
                Vehicle = 29,
                VoicePersona = 30,
                AlwaysLoadedVoicePersona = 31,
                Foliage = 32,
                UiPeg = 33,
                MaterialEffect = 34,
                MaterialPreload = 35,
                SharedBackpack = 36,
                LandmarkLod = 37,
                GpsPreload = 38,
                NumContainerTypes = 39,
            }
            public Container(KaitaiStream p__io, RfgAsm p__parent = null, RfgAsm p__root = null) : base(p__io)
            {
                m_parent = p__parent;
                m_root = p__root;
                _read();
            }
            private void _read()
            {
                _lenName = m_io.ReadU2le();
                _name = System.Text.Encoding.GetEncoding("ASCII").GetString(m_io.ReadBytes(LenName));
                _type = ((ContainerType) m_io.ReadU1());
                _flags = m_io.ReadU2le();
                _numPrimitives = m_io.ReadU2le();
                _dataOffset = m_io.ReadU4le();
                _numSizes = m_io.ReadU4le();
                _lenCompressedData = m_io.ReadU4le();
                _sizes = new List<uint>();
                for (var i = 0; i < NumSizes; i++)
                {
                    _sizes.Add(m_io.ReadU4le());
                }
                _primitives = new List<Primitive>();
                for (var i = 0; i < NumPrimitives; i++)
                {
                    _primitives.Add(new Primitive(m_io, this, m_root));
                }
            }
            private ushort _lenName;
            private string _name;
            private ContainerType _type;
            private ushort _flags;
            private ushort _numPrimitives;
            private uint _dataOffset;
            private uint _numSizes;
            private uint _lenCompressedData;
            private List<uint> _sizes;
            private List<Primitive> _primitives;
            private RfgAsm m_root;
            private RfgAsm m_parent;
            public ushort LenName { get { return _lenName; } }
            public string Name { get { return _name; } }
            public ContainerType Type { get { return _type; } }

            /// <summary>
            /// Kaitai flag-enums are weird, this field is mapped to an enum manually
            /// </summary>
            public ushort Flags { get { return _flags; } }
            public ushort NumPrimitives { get { return _numPrimitives; } }
            public uint DataOffset { get { return _dataOffset; } }
            public uint NumSizes { get { return _numSizes; } }
            public uint LenCompressedData { get { return _lenCompressedData; } }
            public List<uint> Sizes { get { return _sizes; } }
            public List<Primitive> Primitives { get { return _primitives; } }
            public RfgAsm M_Root { get { return m_root; } }
            public RfgAsm M_Parent { get { return m_parent; } }
        }
        public partial class Primitive : KaitaiStruct
        {
            public static Primitive FromFile(string fileName)
            {
                return new Primitive(new KaitaiStream(fileName));
            }


            public enum PrimitiveType
            {
                None = 0,
                Peg = 1,
                Chunk = 2,
                Zone = 3,
                Terrain = 4,
                StaticMesh = 5,
                CharacterMesh = 6,
                FoliageMesh = 7,
                Material = 8,
                ClothSim = 9,
                Vehicle = 10,
                VehicleAudio = 11,
                Vfx = 12,
                Wavebank = 13,
                FoleyBank = 14,
                MeshMorph = 15,
                VoicePersona = 16,
                AnimFile = 17,
                Vdoc = 18,
                LuaScript = 19,
                Localization = 20,
                TerrainHighLod = 21,
                LandmarkLod = 22,
                NumPrimitiveTypes = 23,
            }

            public enum PrimitiveAllocator
            {
                None = 0,
                World = 1,
                ChunkPreload = 2,
                EffectPreload = 3,
                EffectCutscene = 4,
                ItemPreload = 5,
                DecalPreload = 6,
                ClothSimPreload = 7,
                Tod = 8,
                MpEffectPreload = 9,
                MpItemPreload = 10,
                Player = 11,
                Human = 12,
                LargeWeapon = 13,
                SmallWeapon = 14,
                Vehicle = 15,
                LargeLayer = 16,
                SmallLayer = 17,
                HumanVoicePersona = 18,
                AlwaysLoadedHumanVoicePersona = 19,
                Audio = 20,
                Interface = 21,
                Fsm = 22,
                InterfaceStack = 23,
                InterfaceSlot = 24,
                InterfaceMpPreload = 25,
                InterfaceMpSlot = 26,
                MaterialEffect = 27,
                Permanent = 28,
                DlcEffectPreload = 29,
                DlcItemPreload = 30,
                NumAllocatorTypes = 31,
            }
            public Primitive(KaitaiStream p__io, RfgAsm.Container p__parent = null, RfgAsm p__root = null) : base(p__io)
            {
                m_parent = p__parent;
                m_root = p__root;
                _read();
            }
            private void _read()
            {
                _lenName = m_io.ReadU2le();
                _name = System.Text.Encoding.GetEncoding("ASCII").GetString(m_io.ReadBytes(LenName));
                _type = ((PrimitiveType) m_io.ReadU1());
                _allocator = ((PrimitiveAllocator) m_io.ReadU1());
                _flags = m_io.ReadU1();
                _splitExtIndex = m_io.ReadU1();
                _lenCpu = m_io.ReadU4le();
                _lenGpu = m_io.ReadU4le();
            }
            private ushort _lenName;
            private string _name;
            private PrimitiveType _type;
            private PrimitiveAllocator _allocator;
            private byte _flags;
            private byte _splitExtIndex;
            private uint _lenCpu;
            private uint _lenGpu;
            private RfgAsm m_root;
            private RfgAsm.Container m_parent;
            public ushort LenName { get { return _lenName; } }
            public string Name { get { return _name; } }
            public PrimitiveType Type { get { return _type; } }
            public PrimitiveAllocator Allocator { get { return _allocator; } }

            /// <summary>
            /// Kaitai flag-enums are weird, this field is mapped to an enum manually
            /// </summary>
            public byte Flags { get { return _flags; } }

            /// <summary>
            /// 0 or 1, correlates to file being a cpu+gpu pair, BUT NOT ALWAYS
            /// </summary>
            public byte SplitExtIndex { get { return _splitExtIndex; } }
            public uint LenCpu { get { return _lenCpu; } }
            public uint LenGpu { get { return _lenGpu; } }
            public RfgAsm M_Root { get { return m_root; } }
            public RfgAsm.Container M_Parent { get { return m_parent; } }
        }
        private HeaderBlock _header;
        private List<Container> _containers;
        private RfgAsm m_root;
        private KaitaiStruct m_parent;
        public HeaderBlock Header { get { return _header; } }
        public List<Container> Containers { get { return _containers; } }
        public RfgAsm M_Root { get { return m_root; } }
        public KaitaiStruct M_Parent { get { return m_parent; } }
    }
}
