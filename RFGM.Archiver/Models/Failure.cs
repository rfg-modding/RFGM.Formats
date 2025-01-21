using RFGM.Archiver.Models.Messages;

namespace RFGM.Archiver.Models;

public record Failure(IMessage Message, string Reason, Exception? Exception)
{
    public override string ToString() => $"{Reason}\n\n{Exception}\n\n{Message}";
}
