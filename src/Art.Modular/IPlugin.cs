using System.Diagnostics.CodeAnalysis;
using Art.Common;

namespace Art.Modular;

public interface IPlugin
{
    IArtifactToolRegistry ArtifactToolRegistry { get; }

    bool TryLoadTool(ArtifactToolProfile artifactToolProfile, [NotNullWhen(true)] out IArtifactTool? t);

    IEnumerable<ToolDescription> GetToolDescriptions();
}
