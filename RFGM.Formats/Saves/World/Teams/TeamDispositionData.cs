using RFGM.Formats.Streams;

namespace RFGM.Formats.Saves.World.Teams;

public class TeamDispositionData
{
    public TeamDispositions[,] Dispositions = new TeamDispositions[4, 4];

    public enum TeamDispositions
    {
        Hostile = 0,
        Unfriendly = 1,
        None = 2,
        Friendly = 3,
        Allied = 4
    }

    public void Read(Stream stream)
    {
        for (var i = 0; i < Dispositions.GetLength(0); i++)
        {
            for (var j = 0; j < Dispositions.GetLength(1); j++)
            {
                Dispositions[i, j] = stream.ReadValueEnum<TeamDispositions>();
            }
        }
    }

    public void Write(Stream stream)
    {
        for (var i = 0; i < Dispositions.GetLength(0); i++)
        {
            for (var j = 0; j < Dispositions.GetLength(1); j++)
            {
                stream.WriteValueEnum<TeamDispositions>(Dispositions[i, j]);
            }
        }
    }
}