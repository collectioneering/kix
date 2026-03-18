using System.CommandLine;
using Art.Common.Management;
using Art.Tesler.Properties;

namespace Art.Tesler;

public abstract class FindListCommandBase : ToolCommandBase, IToolGroupOrProfileFileOptions
{
    protected TimeProvider TimeProvider;

    protected Option<string> ProfileFileOption;

    protected Option<bool> ListResourceOption;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    protected Option<bool> DetailedOption;

    public FindListCommandBase(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        TimeProvider timeProvider,
        string name,
        string? description = null)
        : base(toolLogHandlerProvider, pluginStore, toolPropertyProvider, name, description)
    {
        TimeProvider = timeProvider;
        ListResourceOption = new Option<bool>("-l", "--list-resource") { Description = "List associated resources" };
        Add(ListResourceOption);
        ProfileFileOption = new Option<string>("-i", "--input") { HelpName = "file", Description = "Profile file" };
        Add(ProfileFileOption);
        ToolOption = new Option<string>("-t", "--tool") { HelpName = "name", Description = "Tool to use or filter profiles by" };
        Add(ToolOption);
        GroupOption = new Option<string>("-g", "--group") { HelpName = "name", Description = "Group to use or filter profiles by" };
        Add(GroupOption);
        DetailedOption = new Option<bool>("--detailed") { Description = "Show detailed information on entries" };
        Add(DetailedOption);
        Validators.Add(v =>
        {
            if (v.GetValue(ProfileFileOption) == null && v.GetValue(ToolOption) == null)
            {
                v.AddError($"At least one of {ProfileFileOption.Aliases.First()} or {ToolOption.Aliases.First()} must be passed.");
            }
        });
    }

    protected abstract Task ExecuteAsync(
        ParseResult parseResult,
        IToolLogHandler logHandler,
        bool listResource,
        bool detailed,
        IArtifactTool tool,
        CancellationToken cancellationToken);

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var profiles = DumpFindListUtil.GetProfiles(this, parseResult);
        (bool getArtifactRetrievalTimestamps, bool getResourceRetrievalTimestamps) = GetArtifactRetrievalOptions(parseResult);
        var logPreferences = GetLogPreferences(parseResult);
        IToolLogHandler logHandler = ToolLogHandlerProvider.GetDefaultToolLogHandler(logPreferences);
        bool listResource = parseResult.GetValue(ListResourceOption);
        bool detailed = parseResult.GetValue(DetailedOption);
        // not expecting any output
        using var adm = new NullArtifactDataManager();
        foreach (var profile in PrepareProfiles(parseResult, profiles))
        {
            // keep registrations local to each run
            using var arm = new InMemoryArtifactRegistrationManager();
            using var tool = await GetToolAsync(profile, arm, adm, TimeProvider, getArtifactRetrievalTimestamps, getResourceRetrievalTimestamps, cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(parseResult, logHandler, listResource, detailed, tool, cancellationToken);
        }
        return 0;
    }

    Option<string> IToolGroupOrProfileFileOptions.ProfileFileOption => ProfileFileOption;
    Option<string> IToolGroupOrProfileFileOptions.ToolOption => ToolOption;
    Option<string> IToolGroupOrProfileFileOptions.GroupOption => GroupOption;
}
