using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art;
using Art.Common;
using Art.EF.Sqlite;

namespace kix.Commands.Database;

internal class DatabaseCommandDelete : DatabaseCommandBase
{
    protected Option<bool> ListOption;

    protected Option<bool> DoDeleteOption;

    public DatabaseCommandDelete(string name, string? description = null) : base(name, description)
    {
        ListOption = new Option<bool>(new[] { "--list" }, "List items.");
        AddOption(ListOption);
        DoDeleteOption = new Option<bool>(new[] { "--do-delete" }, "Perform actual delete.");
        AddOption(DoDeleteOption);
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        using SqliteArtifactRegistrationManager arm = new(context.ParseResult.GetValueForOption(DatabaseOption)!);
        string? tool = context.ParseResult.HasOption(ToolOption) ? context.ParseResult.GetValueForOption(ToolOption) : null;
        string? group = context.ParseResult.HasOption(GroupOption) ? context.ParseResult.GetValueForOption(GroupOption) : null;
        string? toolLike = context.ParseResult.HasOption(ToolLikeOption) ? context.ParseResult.GetValueForOption(ToolLikeOption) : null;
        string? groupLike = context.ParseResult.HasOption(GroupLikeOption) ? context.ParseResult.GetValueForOption(GroupLikeOption) : null;
        string? id = context.ParseResult.HasOption(IdOption) ? context.ParseResult.GetValueForOption(IdOption) : null;
        string? idLike = context.ParseResult.HasOption(IdLikeOption) ? context.ParseResult.GetValueForOption(IdLikeOption) : null;
        string? nameLike = context.ParseResult.HasOption(NameLikeOption) ? context.ParseResult.GetValueForOption(NameLikeOption) : null;
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
