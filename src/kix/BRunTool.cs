using Art;
using CommandLine;
using kix;

namespace Kix;

public class BRunTool
{
    [Option('c', "cookieFile", HelpText = "Cookie file.", MetaValue = "file")] public string? CookieFile { get; set; }

    [Option('p', "property", HelpText = "Properties.", MetaValue = "property")] public IReadOnlyCollection<string> Properties { get; set; } = null!;

    [Option("debug", HelpText = "Debug mode.")] public bool Debug { get; set; }

    protected async Task<ArtifactTool> GetSearchingToolAsync(ArtifactToolProfile artifactToolProfile, CancellationToken cancellationToken = default)
    {
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        return await GetToolAsync(artifactToolProfile, new InMemoryArtifactRegistrationManager(), new NullArtifactDataManager(), cancellationToken);
    }

    protected async Task<ArtifactTool> GetToolAsync(ArtifactToolProfile artifactToolProfile, ArtifactRegistrationManager arm, ArtifactDataManager adm, CancellationToken cancellationToken = default)
    {
        Common.LoadAssemblyForToolString(artifactToolProfile.Tool);
        if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
        artifactToolProfile = artifactToolProfile.GetWithConsoleOptions(CookieFile, Properties);
        ArtifactTool t = await ArtifactTool.PrepareToolAsync(artifactToolProfile, arm, adm, cancellationToken);
        t.DebugMode = Debug;
        return t;
    }
}
