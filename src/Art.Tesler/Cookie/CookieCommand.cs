using System.CommandLine;

namespace Art.Tesler.Cookie;

public class CookieCommand : Command
{
    public CookieCommand(
        IToolLogHandlerProvider toolLogHandlerProvider)
        : this(toolLogHandlerProvider, "cookie", "Perform operations on browser cookies.")
    {
    }

    public CookieCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        string name,
        string? description = null)
        : base(name, description)
    {
        Add(new CookieCommandExtract(toolLogHandlerProvider, "extract", "Extracts cookies."));
    }
}
