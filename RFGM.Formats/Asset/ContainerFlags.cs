namespace RFGM.Formats.Asset;

[Flags]
public enum ContainerFlags : ushort
{
    None = 0,
    Loaded = 1, //Runtime flag. Set right after the container is loaded
    Flag1 = 2,
    Flag2 = 4,
    Flag3 = 8, //Possibly a runtime only flag that means the container + primitives have been read into memory. Not yet confirmed.
    Flag4 = 16,
    Flag5 = 32,
    ReleaseError = 64, //Runtime flag. Set if stream2_container::req_release fails
    //If set then the container will have a list of primitive sizes before the list of primitives. These are duplicates of the HeaderSize and DataSize in each primitive.
    //This might also be the way to indicate whether the container is a str2_pc or a "virtual" container
    HasSizeList = 128, 
    Passive = 256, //If it's true the container is placed into the passive stream queue. It's unknown what "passive" means in this case.
    Flag9 = 512,
    Flag10 = 1024,
    Flag11 = 2048,
    Flag12 = 4096,
    Flag13 = 8192,
    Flag14 = 16384,
    Flag15 = 32768,
}