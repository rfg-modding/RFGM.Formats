namespace RFGM.Formats.Zones;

public enum DistrictFlags : uint
{
    None = 0,
    ZoneWithoutRelationData = 1 << 0, //Set by layer_pc files used by missions and activities. The layer_pc format is the same as the rfgzone_pc format.
    Flag2 = 1 << 1,
    IsLayer = 1 << 2, //For rfgzone_pc files that don't have the relation data section
    Flag4 = 1 << 3,
    Flag5 = 1 << 4,
    Flag6 = 1 << 5,
    Flag7 = 1 << 6,
    Flag8 = 1 << 7,
    Flag9 = 1 << 8,
    Flag10 = 1 << 9,
    Flag11 = 1 << 10,
    Flag12 = 1 << 11,
    Flag13 = 1 << 12,
    Flag14 = 1 << 13,
    Flag15 = 1 << 14,
    Flag16 = 1 << 15,
    Flag17 = 1 << 17,
    Flag18 = 1 << 18,
    Flag19 = 1 << 19,
    Flag20 = 1 << 20,
    Flag21 = 1 << 21,
    Flag22 = 1 << 22,
    Flag23 = 1 << 23,
    Flag24 = 1 << 24,
    Flag25 = 1 << 25,
    Flag26 = 1 << 26,
    Flag27 = 1 << 27,
    Flag28 = 1 << 28,
    Flag29 = 1 << 29,
    Flag30 = 1 << 30,
    Flag31 = (uint)((long)1 << 31),
    Flag32 = uint.MaxValue,
}