using System.CommandLine;

namespace Kix;

public abstract class BVerb : Command
{
    protected BVerb(string name, string? description = null) : base(name, description)
    {
    }
}
