using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art.Common;
using Art.Common.Management;

namespace Art.Tesler;

public abstract class ToolCommandBase<TPluginStore> : CommandBase where TPluginStore : IRegistryStore
{
    protected TPluginStore PluginStore;

    protected Option<string> UserAgentOption;

    protected Option<string> CookieFileOption;

    protected Option<List<string>> PropertiesOption;

    protected ToolCommandBase(TPluginStore pluginStore, string name, string? description = null) : base(name, description)
    {
        PluginStore = pluginStore;
        UserAgentOption = new Option<string>(new[] { "--user-agent" }, "Custom user agent string") { ArgumentHelpName = "user-agent" };
        AddOption(UserAgentOption);
        CookieFileOption = new Option<string>(new[] { "--cookie-file" }, "Cookie file") { ArgumentHelpName = "file" };
        AddOption(CookieFileOption);
        PropertiesOption = new Option<List<string>>(new[] { "-p", "--property" }, "Add a property") { ArgumentHelpName = "key:value", Arity = ArgumentArity.ZeroOrMore };
        AddOption(PropertiesOption);
    }

    protected async Task<IArtifactTool> GetSearchingToolAsync(InvocationContext context, ArtifactToolProfile artifactToolProfile, CancellationToken cancellationToken = default)
    {
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        return await GetToolAsync(context, artifactToolProfile, new InMemoryArtifactRegistrationManager(), new NullArtifactDataManager(), cancellationToken);
    }

    protected async Task<IArtifactTool> GetToolAsync(InvocationContext context, ArtifactToolProfile artifactToolProfile, IArtifactRegistrationManager arm, IArtifactDataManager adm, CancellationToken cancellationToken = default)
    {
        var plugin = PluginStore.LoadRegistry(ArtifactToolProfileUtil.GetID(artifactToolProfile.Tool));
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        string? cookieFile = context.ParseResult.HasOption(CookieFileOption) ? context.ParseResult.GetValueForOption(CookieFileOption) : null;
        string? userAgent = context.ParseResult.HasOption(UserAgentOption) ? context.ParseResult.GetValueForOption(UserAgentOption) : null;
        IEnumerable<string> properties = context.ParseResult.HasOption(PropertiesOption) ? context.ParseResult.GetValueForOption(PropertiesOption)! : Array.Empty<string>();
        artifactToolProfile = artifactToolProfile.GetWithConsoleOptions(properties, cookieFile, userAgent);
        IArtifactTool t = await ArtifactTool.PrepareToolAsync(plugin, artifactToolProfile, arm, adm, cancellationToken);
        return t;
    }
}
