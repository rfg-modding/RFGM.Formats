using RFGM.Formats.Streams;
using RFGM.Formats.Saves.World.Activities;
using RFGM.Formats.Saves.World.Metadata;
using RFGM.Formats.Saves.World.Objects;
using RFGM.Formats.Saves.World.Parsing;
using RFGM.Formats.Saves.World.Player;
using RFGM.Formats.Saves.World.Teams;
using RFGM.Formats.Saves.World.Territory;
using RFGM.Formats.Saves.World.UI;
using RFGM.Formats.Saves.World.VIP;
using static RFGM.Formats.Saves.World.Parsing.Helpers;

namespace RFGM.Formats.Saves;

public class SaveWorld
{
    public uint MagicNumber1; //1397179986
    public uint MagicNumber2; //52
    public string StartZoneName = "";
    public uint TerritoryZonesCount;

    public DestroyedObjData DestroyedObjState = new();
    public MissionsData MissionsState = new();
    public byte[] ScriptSystemState = []; //TODO: Implement
    public TeamProgressionData TeamProgressionState = new();
    public ActivitiesData ActivitiesState = new();
    public RestorableObjData RestorableObjState = new();
    public RaidTargetsData RaidTargetsState = new();
    public VipData VipState = new();
    public SpawnNodeData SpawnNodeState = new();
    public WorldProperties WorldPropertiesState = new();
    public AudioData AudioState = new();
    public UiData UiState = new();
    public RestoredObjData RestoredObjState = new();
    public CriticalAreaData CriticalAreaState = new();
    public PublicAddressData PublicAddressState = new();
    public TeamDispositionData TeamDispositionState = new();
    public byte[] TrackedStatisticsState = []; //TODO: Implement
    public TimeData TimeState = new();
    public ZoneData ZoneState = new();
    public DistrictData DistrictState = new();
    public PlayerPositionData PlayerPositionState = new();
    public PlayerInventoryData PlayerInventoryState = new();
    public PlayerMetadata PlayerMetadataState = new();
    public RoadData RoadDataState = new();
    public EndBufferData EndBuffer = new();

    public void Read(Stream stream, int dlcId)
    {
        MagicNumber1 = stream.ReadUInt32();
        MagicNumber2 = stream.ReadUInt32();
        StartZoneName = stream.ReadLengthPrefixedString16();
        stream.Align(4);
        TerritoryZonesCount = stream.ReadUInt32();

        stream.Skip(8);
        DestroyedObjState.Read(stream);

        stream.Skip(8);
        MissionsState.Read(stream);

        ScriptSystemState = ReadBytesWithOffsets(stream);

        stream.Skip(8);
        TeamProgressionState.Read(stream);

        stream.Skip(8);
        ActivitiesState.Read(stream);

        stream.Skip(8);
        RestorableObjState.Read(stream);

        stream.Skip(8);
        RaidTargetsState.Read(stream);

        stream.Skip(8);
        VipState.Read(stream);

        stream.Skip(8);
        SpawnNodeState.Read(stream);

        stream.Skip(8);
        WorldPropertiesState.Read(stream);

        stream.Skip(8);
        AudioState.Read(stream);

        stream.Skip(8);
        UiState.Read(stream);

        stream.Skip(8);
        RestoredObjState.Read(stream);

        stream.Skip(8);
        CriticalAreaState.Read(stream);

        stream.Skip(8);
        PublicAddressState.Read(stream);

        stream.Skip(8);
        TeamDispositionState.Read(stream);

        TrackedStatisticsState = ReadBytesWithOffsets(stream);

        stream.Skip(8);
        TimeState.Read(stream);

        ZoneState.Read(stream, TerritoryZonesCount);

        stream.Skip(8);
        DistrictState.Read(stream);

        stream.Skip(8);
        PlayerPositionState.Read(stream);

        stream.Skip(8);
        PlayerInventoryState.Read(stream);

        stream.Skip(8);
        PlayerMetadataState.Read(stream, dlcId);

        stream.Skip(8);
        RoadDataState.Read(stream);

        EndBuffer.Read(stream);
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32(MagicNumber1);
        stream.WriteUInt32(MagicNumber2);
        stream.WriteLengthPrefixedString16(StartZoneName);
        stream.Align(4);
        stream.WriteUInt32(TerritoryZonesCount);
        WriteWithOffsets(stream, DestroyedObjState.Write);
        WriteWithOffsets(stream, MissionsState.Write);
        WriteBytesWithOffsets(stream, ScriptSystemState);
        WriteWithOffsets(stream, TeamProgressionState.Write);
        WriteWithOffsets(stream, ActivitiesState.Write);
        WriteWithOffsets(stream, RestorableObjState.Write);
        WriteWithOffsets(stream, RaidTargetsState.Write);
        WriteWithOffsets(stream, VipState.Write);
        WriteWithOffsets(stream, SpawnNodeState.Write);
        WriteWithOffsets(stream, WorldPropertiesState.Write);
        WriteWithOffsets(stream, AudioState.Write);
        WriteWithOffsets(stream, UiState.Write);
        WriteWithOffsets(stream, RestoredObjState.Write);
        WriteWithOffsets(stream, CriticalAreaState.Write);
        WriteWithOffsets(stream, PublicAddressState.Write);
        WriteWithOffsets(stream, TeamDispositionState.Write);
        WriteBytesWithOffsets(stream, TrackedStatisticsState);
        WriteWithOffsets(stream, TimeState.Write);
        ZoneState.Write(stream);
        WriteWithOffsets(stream, DistrictState.Write);
        WriteWithOffsets(stream, PlayerPositionState.Write);
        WriteWithOffsets(stream, PlayerInventoryState.Write);
        WriteWithOffsets(stream, PlayerMetadataState.Write);
        WriteWithOffsets(stream, RoadDataState.Write);
        EndBuffer.Write(stream);
    }
}