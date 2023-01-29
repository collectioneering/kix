using System.CommandLine;
using System.CommandLine.Invocation;
using Art;
using Art.Common;
using Art.EF.Sqlite;

namespace kix.Commands.Database;

internal class DatabaseCommandDelete : DatabaseCommandBase
{
    protected Option<bool> ListOption;
    protected Option<bool> AllOption;

    protected Option<bool> DoDeleteOption;

    public DatabaseCommandDelete(string name, string? description = null) : base(name, description)
    {
        ListOption = new Option<bool>(new[] { "--list" }, "List items");
        AddOption(ListOption);
        AllOption = new Option<bool>(new[] { "--all" }, "List items");
        AddOption(AllOption);
        DoDeleteOption = new Option<bool>(new[] { "--do-delete" }, "Perform actual delete");
        AddOption(DoDeleteOption);
        AddValidator(result =>
        {
            if (result.GetValueForOption(ToolOption) != null) return;
            if (result.GetValueForOption(GroupOption) != null) return;
            if (result.GetValueForOption(ToolLikeOption) != null) return;
            if (result.GetValueForOption(GroupLikeOption) != null) return;
            if (result.GetValueForOption(IdOption) != null) return;
            if (result.GetValueForOption(IdLikeOption) != null) return;
            if (result.GetValueForOption(NameLikeOption) != null) return;
            if (result.GetValueForOption(AllOption)) return;
            result.ErrorMessage = "At least one filter or --all must be specified.";
        });
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        using SqliteArtifactRegistrationManager arm = new(context.ParseResult.GetValueForOption(DatabaseOption)!);
        string? tool = context.ParseResult.GetValueForOption(ToolOption);
        string? group = context.ParseResult.GetValueForOption(GroupOption);
        string? toolLike = context.ParseResult.GetValueForOption(ToolLikeOption);
        string? groupLike = context.ParseResult.GetValueForOption(GroupLikeOption);
        string? id = context.ParseResult.GetValueForOption(IdOption);
        string? idLike = context.ParseResult.GetValueForOption(IdLikeOption);
        string? nameLike = context.ParseResult.GetValueForOption(NameLikeOption);
        IEnumerable<ArtifactInfo> en = (await arm.ListArtifactsOptionalsAsync(tool, group)).WithFilters(tool, toolLike, group, groupLike, id, idLike, nameLike);
        int v = 0;
        bool list = context.ParseResult.GetValueForOption(ListOption);
        bool doDelete = context.ParseResult.GetValueForOption(DoDeleteOption);
        bool listResource = context.ParseResult.GetValueForOption(ListResourceOption);
        bool detailed = context.ParseResult.GetValueForOption(DetailedOption);
        foreach (ArtifactInfo i in en.ToList())
        {
            if (list) await Common.DisplayAsync(i, listResource, arm, detailed);
            if (doDelete) await arm.RemoveArtifactAsync(i.Key);
            v++;
        }
        Console.WriteLine(doDelete ? $"Deleted {v} records." : $"{v} records would be affected.");
        return 0;
    }
}
