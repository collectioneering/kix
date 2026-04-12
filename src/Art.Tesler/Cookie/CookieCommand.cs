using System.CommandLine;

namespace Art.Tesler.Cookie;

public class CookieCommand : Command
{
    public CookieCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IExtensionsContext extensionsContext)
        : this(toolLogHandlerProvider, extensionsContext, "cookie", "Perform operations on browser cookies.")
    {
    }

    public CookieCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IExtensionsContext extensionsContext,
        string name,
        string? description = null)
        : base(name, description)
    {
        Add(new CookieCommandExtract(toolLogHandlerProvider, extensionsContext, "extract", "Extracts cookies."));
    }
}
