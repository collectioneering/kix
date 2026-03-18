using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Art.Tesler;

internal static class PurificationUtil
{
    public static bool TryIdentify(IEnumerable<IArtifactToolSelectableRegistry<string>> selectableRegistries, string key, [NotNullWhen(true)] out ArtifactToolProfile? profile)
    {
        foreach (var registry in selectableRegistries)
        {
            if (registry.TryIdentify(key, out var artifactToolId, out string? artifactId))
            {
                var opts = new Dictionary<string, JsonElement> { { "artifactList", JsonSerializer.SerializeToElement([artifactId], SourceGenerationContext.s_context.ListString) } };
                profile = new ArtifactToolProfile(artifactToolId.GetToolString(), null, opts);
                return true;
            }
        }
        profile = null;
        return false;
    }
}
