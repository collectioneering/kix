using Art;
using Art.Common;
using Art.EF.Sqlite;
using CommandLine;

namespace Kix;

[Verb("db-delete", HelpText = "Delete archives in database.")]
internal class RunDbDelete : RunDbBase, IRunnable
{
    [Option("list", HelpText = "List items.")]
    public bool List { get; set; }

    [Option("do-delete", HelpText = "Perform actual delete.")]
    public bool DoDelete { get; set; }

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
