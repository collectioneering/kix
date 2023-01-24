using System.CommandLine;

namespace Kix;

public abstract class BRun : BVerb
{
    protected Option<bool> VerboseOption;

    protected Option<bool> IgnoreSharedAssemblyVersionOption;

    protected BRun(string name, string? description = null) : base(name, description)
    {
        VerboseOption = new Option<bool>(new[] { "-v", "--verbose" }, "Enable verbose output.");
        AddOption(VerboseOption);
        IgnoreSharedAssemblyVersionOption = new Option<bool>(new[] { "--ignore-shared-api-version" }, "Ignore Art library version validation.");
        AddOption(IgnoreSharedAssemblyVersionOption);
    }
}
