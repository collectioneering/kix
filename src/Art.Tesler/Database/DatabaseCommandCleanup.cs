using System.CommandLine;

namespace Art.Tesler.Database;

public class DatabaseCommandCleanup : CommandBase
{
    protected ITeslerRegistrationProvider RegistrationProvider;

    public DatabaseCommandCleanup(
        IOutputControl toolOutput,
        ITeslerRegistrationProvider registrationProvider,
        string name,
        string? description = null)
        : base(toolOutput, name, description)
    {
        RegistrationProvider = registrationProvider;
        RegistrationProvider.Initialize(this);
    }

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var arm = RegistrationProvider.CreateArtifactRegistrationManager(parseResult);
        if (arm is not IArtifactRegistrationManagerCleanup armCleanup)
        {
            ToolOutput.Error.WriteLine($"Artifact registration provider {arm} does not support cleanup.");
            return 5;
        }
        await armCleanup.CleanupDatabaseAsync(cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
