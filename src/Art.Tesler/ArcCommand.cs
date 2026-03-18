using System.CommandLine;
using Art.Common;
using Art.Common.Proxies;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler;

public class ArcCommand : ArcDumpCommandBase
{
    protected IProfileResolver ProfileResolver;

    protected Argument<List<string>> ProfileFilesArg;

    protected Option<ResourceUpdateMode> UpdateOption;

    protected Option<bool> FullOption;

    protected Option<ArtifactSkipMode> SkipOption;

    protected Option<bool> FastExitOption;

    public ArcCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
        : this(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, profileResolver, "arc", "Execute archival artifact tools.")
    {
    }

    public ArcCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver,
        string name,
        string? description = null)
        : base(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, name, description)
    {
        ProfileResolver = profileResolver;
        ProfileFilesArg = new Argument<List<string>>("profile") { HelpName = "profile", Arity = ArgumentArity.OneOrMore, Description = "Profile file(s)" };
        Add(ProfileFilesArg);
        UpdateOption = new Option<ResourceUpdateMode>("-u", "--update") { HelpName = "mode", Description = $"Resource update mode ({Common.ResourceUpdateModes})" };
        UpdateOption.DefaultValueFactory = static _ => ResourceUpdateMode.ArtifactHard;
        Add(UpdateOption);
        FullOption = new Option<bool>("-f", "--full") { Description = "Only process full artifacts" };
        Add(FullOption);
        SkipOption = new Option<ArtifactSkipMode>("-s", "--skip") { Description = $"Skip artifacts ({Common.ArtifactSkipModes})" };
        SkipOption.HelpName = "mode";
        SkipOption.DefaultValueFactory = static _ => ArtifactSkipMode.None;
        Add(SkipOption);
        FastExitOption = new Option<bool>("-z", "--fast-exit") { Description = $"Equivalent to -s/--skip {nameof(ArtifactSkipMode.FastExit)}" };
        Add(FastExitOption);
    }

    protected override IReadOnlyList<ArtifactToolProfile> GetProfiles(ParseResult parseResult)
    {
        List<ArtifactToolProfile> profiles = [];
        foreach (string profileFile in parseResult.GetRequiredValue(ProfileFilesArg))
        {
            ResolveAndAddProfiles(ProfileResolver, profiles, profileFile);
        }
        return profiles;
    }

    protected override ArtifactToolDumpOptions GetArtifactToolDumpOptions(ParseResult parseResult, ChecksumSource? checksumSource)
    {
        ResourceUpdateMode update = parseResult.GetValue(UpdateOption);
        bool full = parseResult.GetValue(FullOption);
        ArtifactSkipMode skip = parseResult.GetValue(SkipOption);
        bool fastExit = parseResult.GetValue(FastExitOption);
        return new ArtifactToolDumpOptions(
            update,
            !full,
            fastExit ? ArtifactSkipMode.FastExit : skip,
            checksumSource);
    }
}
