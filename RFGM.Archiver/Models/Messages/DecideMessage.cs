namespace RFGM.Archiver.Models.Messages;

public record DecideMessage(string Input, string? Output, PackSettings? PackSettings, UnpackSettings? UnpackSettings) : IMessage;
