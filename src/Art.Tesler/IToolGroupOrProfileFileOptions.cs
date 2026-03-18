using System.CommandLine;

namespace Art.Tesler;

public interface IToolGroupOrProfileFileOptions
{
    Option<string> ProfileFileOption { get; }

    Option<string> ToolOption { get; }

    Option<string> GroupOption { get; }
}
