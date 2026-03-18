namespace Art.Tesler;

public record ArtifactDataObjectLog(string? Title, string? Body, LogLevel Level, IArtifactData ArtifactData) : ObjectLog(Title, Body, Level);
