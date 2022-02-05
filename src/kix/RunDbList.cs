using Art;
using Art.EF.Sqlite;
using CommandLine;

namespace Kix;

[Verb("db-list", HelpText = "List archives in database.")]
internal class RunDbList : RunDbBase, IRunnable
{
    public async Task<int> RunAsync()
    {
        using SqliteArtifactRegistrationManager arm = new(Database);
        IEnumerable<ArtifactInfo> en = (await arm.ListArtifactsOptionalsAsync(Tool, Group)).WithFilters(Tool, ToolLike, Group, GroupLike, Id, IdLike, NameLike);
        foreach (ArtifactInfo i in en)
            await Common.DisplayAsync(i, ListResource, arm, Detailed);
        return 0;
    }
}
