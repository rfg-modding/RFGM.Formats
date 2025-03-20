using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Activities;

public class ActivitiesData
{
    private const int ActivityCount = 16; //TODO: Make count 17. Currently missing a uint in the expected end position so will set to 16 and read half of the last ActivityBase.
    
    public List<ActivityBase> Activities = new();
    public uint ActivityNodeStartOnLoad;

    public class ActivityBase
    {
        public uint DisableFlags;
        public uint SuspendFlags;
    }

    public void Read(Stream stream)
    {
        ActivityNodeStartOnLoad = stream.ReadUInt32();
        for (var i = 0; i < ActivityCount; ++i)
        {
            Activities.Add(new ActivityBase
            {
                DisableFlags = stream.ReadUInt32(),
                SuspendFlags = stream.ReadUInt32()
            });
        }

        stream.ReadUInt32();
    }

    public void Write(Stream stream)
    {
        stream.WriteUInt32(ActivityNodeStartOnLoad);
        foreach (var activity in Activities)
        {
            stream.WriteUInt32(activity.DisableFlags);
            stream.WriteUInt32(activity.SuspendFlags);
        }

        stream.WriteUInt32(0);
    }

}