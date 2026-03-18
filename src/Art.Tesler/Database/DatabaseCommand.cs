using System.CommandLine;

namespace Art.Tesler.Database;

public class DatabaseCommand : Command
{
    public DatabaseCommand(
        IOutputControl toolOutput,
        ITeslerRegistrationProvider registrationProvider,
        ITeslerRegistrationProvider inputRegistrationProvider)
        : this(toolOutput, registrationProvider, inputRegistrationProvider, "db", "Perform operations on database.")
    {
    }

    public DatabaseCommand(
        IOutputControl toolOutput,
        ITeslerRegistrationProvider registrationProvider,
        ITeslerRegistrationProvider inputRegistrationProvider,
        string name,
        string? description = null)
        : base(name, description)
    {
        Add(new DatabaseCommandList(toolOutput, registrationProvider, "list", "List archives in database."));
        if (typeof(IArtifactRegistrationManagerCleanup).IsAssignableFrom(registrationProvider.GetArtifactRegistrationManagerType()))
        {
            Add(new DatabaseCommandCleanup(toolOutput, registrationProvider, "cleanup", "Cleanup database."));
        }
        Add(new DatabaseCommandDelete(toolOutput, registrationProvider, "delete", "Delete archives in database."));
        Add(new DatabaseCommandMerge(toolOutput, registrationProvider, inputRegistrationProvider, "merge", "Merge from another database."));
    }
}
