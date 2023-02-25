using System.CommandLine;

namespace Art.Tesler.Database;

public class DatabaseCommand : Command
{
    public DatabaseCommand() : this("db", "Perform operations on database.")
    {
    }

    public DatabaseCommand(string name, string? description = null) : base(name, description)
    {
        AddCommand(new DatabaseCommandList("list", "List archives in database."));
        AddCommand(new DatabaseCommandDelete("delete", "Delete archives in database."));
    }
}
