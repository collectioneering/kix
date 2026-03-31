using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Art.Common;

namespace Art.Tesler;

public record Refactorings(
    [property: JsonPropertyName("types")] IReadOnlyDictionary<string, string> Types,
    [property: JsonPropertyName("assemblies")] IReadOnlyDictionary<string, RefactoringsAssembly> Assemblies
)
{
    public bool TryGetRefactoredArtifactKey(ArtifactKey inputArtifactKey, [NotNullWhen(true)] out ArtifactKey? refactoredArtifactKey)
    {
        (string toolAssembly, string toolType) = ArtifactToolIDUtil.ParseID(inputArtifactKey.Tool);
        if (Types.TryGetValue(inputArtifactKey.Tool, out string? refactoredTool))
        {
            refactoredArtifactKey = inputArtifactKey with { Tool = refactoredTool };
            return true;
        }
        if (Assemblies.TryGetValue(toolAssembly, out var refactoredAssembly))
        {
            if (refactoredAssembly.UpdateNamespace)
            {
                if (refactoredAssembly.Value.Length > 0 && toolType.StartsWith(toolAssembly))
                {
                    var slice = toolType.AsSpan(toolAssembly.Length);
                    if (slice.StartsWith(".", StringComparison.InvariantCulture))
                    {
                        refactoredArtifactKey = inputArtifactKey with { Tool = ArtifactToolIDUtil.CreateToolString(refactoredAssembly.Value, $"{refactoredAssembly.Value}.{slice[1..]}") };
                        return true;
                    }
                }
            }
            refactoredArtifactKey = inputArtifactKey with { Tool = ArtifactToolIDUtil.CreateToolString(refactoredAssembly.Value, toolType) };
            return true;
        }
        refactoredArtifactKey = null;
        return false;
    }

}
public record RefactoringsAssembly(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("update_namespace")] bool UpdateNamespace
);
