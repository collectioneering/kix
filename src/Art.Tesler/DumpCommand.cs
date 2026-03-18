using System.CommandLine;
using Art.Common;
using Art.Common.Proxies;
using Art.Tesler.Properties;

namespace Art.Tesler;

public class DumpCommand : ArcDumpCommandBase, IToolGroupOrProfileFileOptions
{
    protected Option<string> ProfileFileOption;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    public DumpCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        TimeProvider timeProvider)
        : this(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, "dump", "Execute artifact dump tools.")
    {
    }

    public DumpCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        TimeProvider timeProvider,
        string name,
        string? description = null)
        : base(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, name, description)
    {
        ProfileFileOption = new Option<string>("-i", "--input") { HelpName = "file", Description = "Profile file" };
        Add(ProfileFileOption);
        ToolOption = new Option<string>("-t", "--tool") { HelpName = "name", Description = "Tool to use or filter profiles by" };
        Add(ToolOption);
        GroupOption = new Option<string>("-g", "--group") { HelpName = "name", Description = "Group to use or filter profiles by" };
        Add(GroupOption);
        Validators.Add(v =>
        {
            if (v.GetValue(ProfileFileOption) == null && v.GetValue(ToolOption) == null)
            {
                v.AddError($"At least one of {ProfileFileOption.Aliases.First()} or {ToolOption.Aliases.First()} must be passed.");
            }
        });
    }

    protected override IReadOnlyList<ArtifactToolProfile> GetProfiles(ParseResult parseResult)
    {
        return DumpFindListUtil.GetProfiles(this, parseResult);
    }

    protected override ArtifactToolDumpOptions GetArtifactToolDumpOptions(ParseResult parseResult, ChecksumSource? checksumSource)
    {
        return new ArtifactToolDumpOptions(
            ResourceUpdateMode.Hard,
            true,
            ArtifactSkipMode.None,
            checksumSource);
    }

    Option<string> IToolGroupOrProfileFileOptions.ProfileFileOption => ProfileFileOption;
    Option<string> IToolGroupOrProfileFileOptions.ToolOption => ToolOption;
    Option<string> IToolGroupOrProfileFileOptions.GroupOption => GroupOption;
}
