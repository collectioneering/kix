using System.CommandLine;

namespace Art.Tesler;

public interface ITeslerRegistrationProvider
{
    void Initialize(Command command);

    Type GetArtifactRegistrationManagerType();

    IArtifactRegistrationManager CreateArtifactRegistrationManager(ParseResult parseResult);
}
