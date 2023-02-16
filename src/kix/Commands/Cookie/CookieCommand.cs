using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace kix.Commands.Cookie;

public class CookieCommand : Command
{
    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public CookieCommand() : this("cookie", "Perform operations on browser cookies.")
    {
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public CookieCommand(string name, string? description = null) : base(name, description)
    {
        AddCommand(new CookieCommandExtract("extract", "Extracts cookies."));
    }
}
