using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace kix.Commands.Database;

public class DatabaseCommand : Command
{
    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public DatabaseCommand() : this("db", "Perform operations on database.")
    {
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public DatabaseCommand(string name, string? description = null) : base(name, description)
    {
        AddCommand(new DatabaseCommandList("list", "List archives in database."));
        AddCommand(new DatabaseCommandDelete("delete", "Delete archives in database."));
    }
}
