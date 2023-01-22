using Art;
using Art.Common;
using Art.Common.Management;
using CommandLine;

namespace Kix;

public class BRunTool
{
    [Option('c', "cookieFile", HelpText = "Cookie file.", MetaValue = "file")]
    public string? CookieFile { get; set; }

    [Option('p', "property", HelpText = "Properties.", MetaValue = "property")]
    public IReadOnlyCollection<string> Properties { get; set; } = null!;

    protected async Task<ArtifactToolBase> GetSearchingToolAsync(ArtifactToolProfile artifactToolProfile, CancellationToken cancellationToken = default)
    {
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        return await GetToolAsync(artifactToolProfile, new InMemoryArtifactRegistrationManager(), new NullArtifactDataManager(), cancellationToken);
    }

    protected async Task<ArtifactToolBase> GetToolAsync(ArtifactToolProfile artifactToolProfile, ArtifactRegistrationManagerBase arm, ArtifactDataManager adm, CancellationToken cancellationToken = default)
    {
        Common.LoadAssemblyForToolString(artifactToolProfile.Tool);
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        artifactToolProfile = artifactToolProfile.GetWithConsoleOptions(CookieFile, Properties);
        ArtifactToolBase t = await ArtifactTool.PrepareToolAsync(artifactToolProfile, arm, adm, cancellationToken);
        return t;
    }
}
