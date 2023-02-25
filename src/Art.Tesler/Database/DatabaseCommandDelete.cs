using System.CommandLine;
using System.CommandLine.Invocation;
using Art.Common;
using Art.EF.Sqlite;

namespace Art.Tesler.Database;

internal class DatabaseCommandDelete : DatabaseCommandBase
{
    protected Option<bool> ListOption;

    protected Option<bool> AllOption;

    protected Option<bool> DoDeleteOption;

    public DatabaseCommandDelete(string name, string? description = null) : base(name, description)
    {
        ListOption = new Option<bool>(new[] { "--list" }, "List items");
        AddOption(ListOption);
        AllOption = new Option<bool>(new[] { "--all" }, "Delete all items");
        AddOption(AllOption);
        DoDeleteOption = new Option<bool>(new[] { "--do-delete" }, "Perform actual delete");
        AddOption(DoDeleteOption);
        AddValidator(result =>
        {
            bool anyFilters = false;
            anyFilters |= result.GetValueForOption(ToolOption) != null;
            anyFilters |= result.GetValueForOption(GroupOption) != null;
            anyFilters |= result.GetValueForOption(ToolLikeOption) != null;
            anyFilters |= result.GetValueForOption(GroupLikeOption) != null;
            anyFilters |= result.GetValueForOption(IdOption) != null;
            anyFilters |= result.GetValueForOption(IdLikeOption) != null;
            anyFilters |= result.GetValueForOption(NameLikeOption) != null;
            if (result.GetValueForOption(AllOption))
            {
                if (anyFilters)
                {
                    result.ErrorMessage = "Cannot specify --all when filters have been specified.";
                }
            }
            else if (!anyFilters)
            {
                result.ErrorMessage = "At least one filter or --all must be specified.";
            }
        });
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        using SqliteArtifactRegistrationManager arm = new(context.ParseResult.GetValueForOption(DatabaseOption)!);
        IEnumerable<ArtifactInfo> en;
        if (context.ParseResult.GetValueForOption(AllOption))
        {
            en = await arm.ListArtifactsAsync();
        }
        else
        {
            string? tool = context.ParseResult.GetValueForOption(ToolOption);
            string? group = context.ParseResult.GetValueForOption(GroupOption);
            string? toolLike = context.ParseResult.GetValueForOption(ToolLikeOption);
            string? groupLike = context.ParseResult.GetValueForOption(GroupLikeOption);
            string? id = context.ParseResult.GetValueForOption(IdOption);
            string? idLike = context.ParseResult.GetValueForOption(IdLikeOption);
            string? nameLike = context.ParseResult.GetValueForOption(NameLikeOption);
            en = (await arm.ListArtifactsOptionalsAsync(tool, group)).WithFilters(tool, toolLike, group, groupLike, id, idLike, nameLike);
        }
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
