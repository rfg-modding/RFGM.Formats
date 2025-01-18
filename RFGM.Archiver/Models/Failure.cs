namespace RFGM.Archiver.Models;

public record Failure(IMessage Message, string Reason, Exception? Exception);