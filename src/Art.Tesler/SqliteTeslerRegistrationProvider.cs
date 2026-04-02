using System.CommandLine;
using Art.EF.Sqlite;

namespace Art.Tesler;

public class SqliteTeslerRegistrationProvider : ITeslerRegistrationProvider
{
    protected Option<string> DatabaseOption;

    public SqliteTeslerRegistrationProvider()
    {
        DatabaseOption = new Option<string>("-d", "--database") { HelpName = "file", Description = "Sqlite database file", DefaultValueFactory = static _ => Common.DefaultDbFile };
    }

    public SqliteTeslerRegistrationProvider(Option<string> databaseOption)
    {
        DatabaseOption = databaseOption;
    }

    public void Initialize(Command command)
    {
        command.Add(DatabaseOption);
    }

    public Type GetArtifactRegistrationManagerType() => typeof(SqliteArtifactRegistrationManager);

    public IArtifactRegistrationManager CreateArtifactRegistrationManager(ParseResult parseResult, bool isReadonly = false)
    {
        var config = new SqliteArtifactRegistrationManagerConfig(ApplyMigrationsOnStartup: !isReadonly, IsReadOnly: isReadonly, DisablePendingMigrationsCheck: false);
        return new SqliteArtifactRegistrationManager(parseResult.GetRequiredValue(DatabaseOption), config);
    }
}
