using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art;
using Art.Common;
using Art.Common.Management;
using Art.Modular;

namespace kix.Commands;

public abstract class ToolCommandBase : CommandBase
{
    protected Option<string> CookieFileOption;

    protected Option<List<string>> PropertiesOption;

    protected ToolCommandBase(string name, string? description = null) : base(name, description)
    {
        CookieFileOption = new Option<string>(new[] { "-c", "--cookie-file" }, "Cookie file.") { ArgumentHelpName = "file" };
        AddOption(CookieFileOption);
        PropertiesOption = new Option<List<string>>(new[] { "-p", "--property" }, "Add a property.") { ArgumentHelpName = "key:value", Arity = ArgumentArity.ZeroOrMore };
        AddOption(PropertiesOption);
    }

    protected async Task<IArtifactTool> GetSearchingToolAsync(InvocationContext context, ArtifactToolProfile artifactToolProfile, CancellationToken cancellationToken = default)
    {
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        return await GetToolAsync(context, artifactToolProfile, new InMemoryArtifactRegistrationManager(), new NullArtifactDataManager(), cancellationToken);
    }

    protected async Task<IArtifactTool> GetToolAsync(InvocationContext context, ArtifactToolProfile artifactToolProfile, IArtifactRegistrationManager arm, IArtifactDataManager adm, CancellationToken cancellationToken = default)
    {
        var plugin = Plugin.LoadForToolString(artifactToolProfile.Tool);
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        string? cookieFile = context.ParseResult.HasOption(CookieFileOption) ? context.ParseResult.GetValueForOption(CookieFileOption) : null;
        IEnumerable<string> properties = context.ParseResult.HasOption(PropertiesOption) ? context.ParseResult.GetValueForOption(PropertiesOption)! : Array.Empty<string>();
        artifactToolProfile = artifactToolProfile.GetWithConsoleOptions(cookieFile, properties);
        IArtifactTool t = await ArtifactTool.PrepareToolAsync(plugin.Context, artifactToolProfile, arm, adm, cancellationToken);
        return t;
    }
}
