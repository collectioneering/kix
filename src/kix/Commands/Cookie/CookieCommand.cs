using System.CommandLine;

namespace kix.Commands.Cookie;

public class CookieCommand : Command
{
    public CookieCommand() : this("cookie", "Perform operations on browser cookies.")
    {
    }

    public CookieCommand(string name, string? description = null) : base(name, description)
    {
        AddCommand(new CookieCommandExtract("extract", "Extracts cookies."));
    }
}
