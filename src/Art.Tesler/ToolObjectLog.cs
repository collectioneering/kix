namespace Art.Tesler;

public record ToolObjectLog(string? Title, string? Body, LogLevel Level, string Tool, string Group) : ObjectLog(Title, Body, Level);
