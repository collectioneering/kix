using Art;
using Art.Common;
using Art.Common.Management;
using CommandLine;

namespace Kix;

public class BRunTool : BRun
{
    [Option('c', "cookie-file", HelpText = "Cookie file.", MetaValue = "file")]
    public string? CookieFile { get; set; }

    [Option('p', "property", HelpText = "Properties.", MetaValue = "property")]
    public IReadOnlyCollection<string> Properties { get; set; } = null!;

    protected async Task<IArtifactTool> GetSearchingToolAsync(ArtifactToolProfile artifactToolProfile, CancellationToken cancellationToken = default)
    {
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        return await GetToolAsync(artifactToolProfile, new InMemoryArtifactRegistrationManager(), new NullArtifactDataManager(), cancellationToken);
    }

    protected async Task<IArtifactTool> GetToolAsync(ArtifactToolProfile artifactToolProfile, IArtifactRegistrationManager arm, IArtifactDataManager adm, CancellationToken cancellationToken = default)
    {
        var context = Plugin.LoadForToolString(artifactToolProfile.Tool, !IgnoreSharedAssemblyVersion);
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        artifactToolProfile = artifactToolProfile.GetWithConsoleOptions(CookieFile, Properties);
        IArtifactTool t = await ArtifactTool.PrepareToolAsync(context.Context, artifactToolProfile, arm, adm, cancellationToken);
        return t;
    }
}
