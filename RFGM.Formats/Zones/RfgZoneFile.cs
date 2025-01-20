using RFGM.Formats.Hashes;
using RFGM.Formats.Streams;

namespace RFGM.Formats.Zones;

public class RfgZoneFile
{
    public const uint ExpectedSignature = 1162760026;
    public const uint ExpectedVersion = 36;
    public const uint RelationDataSize = 87368;

    public struct ZoneHeader
    {
        public uint Signature;
        public uint Version;
        public uint NumObjects;
        public uint NumHandles;
        public uint DistrictHash;
        public DistrictFlags DistrictFlags;
    }

    //This is the data that sometimes follows the header in zone files
    //Takes up 87368 in the file
    public struct RelationData
    {
        //4 bytes padding
        public ushort Free = 0;
        public ushort[] Slot = new ushort[7280];

        public ushort[] Next = new ushort[7280];

        //2 bytes padding
        public uint[] Keys = new uint[7280];
        public uint[] Values = new uint[7280];

        public RelationData()
        {
        }
    }

    public string Name;
    
    public ZoneHeader Header;

    public bool HasRelationData = false;

    //public RelationData RelationData;
    public List<RfgZoneObject> Objects = new();

    public string DistrictName
    {
        get
        {
            if (Header.DistrictHash == 0)
            {
                return "None";
            }

            return HashDictionary.FindOriginString(Header.DistrictHash) ?? "Unknown";
        }
    }

    public RfgZoneFile(string name)
    {
        Name = name;
    }
    
    public bool Read(Stream stream, out string error)
    {
        error = string.Empty;
        try
        {
            Header = stream.ReadStruct<ZoneHeader>();
            if (Header.Signature != ExpectedSignature)
            {
                throw new Exception($"Encountered unexpected zone file signature {Header.Signature}. Expected {ExpectedSignature}.");
            }

            if (Header.Version != ExpectedVersion)
            {
                throw new Exception($"Encountered unexpected zone file version {Header.Version}. Expected {ExpectedVersion}.");
            }

            HasRelationData = !Header.DistrictFlags.HasFlag(DistrictFlags.ZoneWithoutRelationData) && !Header.DistrictFlags.HasFlag(DistrictFlags.IsLayer);
            if (HasRelationData)
            {
                stream.Skip(RelationDataSize);
            }

            for (int i = 0; i < Header.NumObjects; i++)
            {
                RfgZoneObject obj = new();
                if (!obj.Load(stream, out string objectError))
                {
                    error = $"Failed to load object {i}. Error:{Environment.NewLine}{objectError}";
                    return false;
                }

                Objects.Add(obj);
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}