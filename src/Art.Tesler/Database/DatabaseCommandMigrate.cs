using System.CommandLine;

namespace Art.Tesler.Database;

public class DatabaseCommandMigrate : DatabaseCommandBase
{
    public DatabaseCommandMigrate(
        IOutputControl toolOutput,
        ITeslerRegistrationProvider registrationProvider,
        string name,
        string? description = null)
        : base(toolOutput, registrationProvider, name, description)
    {
    }

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var arm = RegistrationProvider.CreateArtifactRegistrationManager(parseResult, isReadonly: false);
        return 0;
    }
}
