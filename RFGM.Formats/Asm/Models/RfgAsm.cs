namespace RFGM.Formats.Asm.Models;

public partial class RfgAsm
{
    public partial class Container
    {
        public ContainerFlags FlagsEnum => (ContainerFlags) _flags;

        [Flags]
        public enum ContainerFlags : ushort
        {
            None = 0,
            /// <summary>
            /// Runtime flag. Set right after the container is loaded
            /// </summary>
            Loaded = 1,
            Flag1 = 2,
            Flag2 = 4,
            /// <summary>
            /// Possibly a runtime only flag that means the container + primitives have been read into memory. Not yet confirmed.
            /// </summary>
            Flag3 = 8,
            Flag4 = 16,
            Flag5 = 32,
            /// <summary>
            /// Runtime flag. Set if stream2_container::req_release fails
            /// </summary>
            ReleaseError = 64,
            Flag7 = 128,
            /// <summary>
            /// If it's true the container is placed into the passive stream queue. It's unknown what "passive" means in this case.
            /// </summary>
            Passive = 256,
            Flag9 = 512,
            Flag10 = 1024,
            Flag11 = 2048,
            Flag12 = 4096,
            Flag13 = 8192,
            Flag14 = 16384,
            Flag15 = 32768,
        };
    }

    public partial class Primitive
    {
        public PrimitiveFlags FlagsEnum => (PrimitiveFlags) _flags;

        [Flags]
        public enum PrimitiveFlags : byte
        {
            None = 0,

            /// <summary>
            /// Runtime flag. Set right after the primitive is successfully loaded.
            /// </summary>
            Loaded = 1,
            Flag1 = 2,

            /// <summary>
            /// Primitive is split into cpu/gpu files
            /// </summary>
            Split = 4,
            Flag3 = 8,
            Flag4 = 16,
            Flag5 = 32,

            /// <summary>
            /// Runtime flag. Set if stream2_container::req_release fails
            /// </summary>
            ReleaseError = 64,
            Flag7 = 128,
        }
    }
}
