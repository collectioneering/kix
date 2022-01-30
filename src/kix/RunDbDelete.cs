using Art;
using Art.EF.Sqlite;
using CommandLine;

namespace Kix;

[Verb("db-delete", HelpText = "Delete archives in database.")]
internal class RunDbDelete : IRunnable
{
    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = "kix_data.db")]
    public string Database { get; set; } = null!;

    [Option('t', "tool", HelpText = "Tool to filter by.", MetaValue = "pattern")]
    public string? Tool { get; set; }

    [Option('g', "group", HelpText = "Group to filter by.", MetaValue = "pattern")]
    public string? Group { get; set; }

    [Option('i', "id", HelpText = "Id to filter by.", MetaValue = "pattern")]
    public string? Id { get; set; }

    [Option("tool-like", HelpText = "Tool pattern to filter by.", MetaValue = "pattern")]
    public string? ToolLike { get; set; }

    [Option("group-like", HelpText = "Group pattern to filter by.", MetaValue = "pattern")]
    public string? GroupLike { get; set; }

    [Option("id-like", HelpText = "Id pattern to filter by.", MetaValue = "pattern")]
    public string? IdLike { get; set; }

    [Option("name-like", HelpText = "Name pattern to filter by.", MetaValue = "pattern")]
    public string? NameLike { get; set; }

    [Option('l', "list-resource", HelpText = "List resource items.")]
    public bool ListResource { get; set; }

    [Option("list", HelpText = "List items.", Group = "action")]
    public bool List { get; set; }

    [Option("do-delete", HelpText = "Perform actual delete.")]
    public bool DoDelete { get; set; }

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

    public async Task<int> RunAsync()
    {
        using SqliteArtifactRegistrationManager arm = new(Database);
        IEnumerable<ArtifactInfo> en = (await arm.ListArtifactsOptionalsAsync(Tool, Group)).WithFilters(Tool, ToolLike, Group, GroupLike, Id, IdLike, NameLike);
        int v = 0;
        foreach (ArtifactInfo i in en.ToList())
        {
            if (List) await Common.DisplayAsync(i, ListResource, arm, Detailed);
            if (DoDelete) await arm.RemoveArtifactAsync(i.Key);
            v++;
        }
        Console.WriteLine(DoDelete ? $"Deleted {v} records." : $"{v} records would be affected.");
        return 0;
    }
}
