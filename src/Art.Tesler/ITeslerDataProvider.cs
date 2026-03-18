using System.CommandLine;

namespace Art.Tesler;

public interface ITeslerDataProvider
{
    void Initialize(Command command);

    IArtifactDataManager CreateArtifactDataManager(ParseResult parseResult);
}
