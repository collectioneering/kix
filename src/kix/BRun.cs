using System.CommandLine.Invocation;

namespace Kix;

public abstract class BRun : BVerb
{
    //protected Option<bool> IgnoreSharedAssemblyVersionOption;

    protected BRun(string name, string? description = null) : base(name, description)
    {
        //IgnoreSharedAssemblyVersionOption = new Option<bool>(new[] { "--ignore-shared-api-version" }, "Ignore Art library version validation.");
        //AddOption(IgnoreSharedAssemblyVersionOption);
    }

    public bool ShouldIgnoreSharedAssemblyVersionOption(InvocationContext invocationContext)
    {
        return false;
        //return invocationContext.ParseResult.GetValueForOption(IgnoreSharedAssemblyVersionOption);
    }
}
