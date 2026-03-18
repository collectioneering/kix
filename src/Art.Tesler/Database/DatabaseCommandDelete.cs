using System.CommandLine;
using Art.Common;

namespace Art.Tesler.Database;

public class DatabaseCommandDelete : DatabaseCommandBase
{
    protected Option<bool> ListOption;

    protected Option<bool> AllOption;

    protected Option<bool> DoDeleteOption;

    public DatabaseCommandDelete(
        IOutputControl toolOutput,
        ITeslerRegistrationProvider registrationProvider,
        string name,
        string? description = null)
        : base(toolOutput, registrationProvider, name, description)
    {
        ListOption = new Option<bool>("--list") { Description = "List items" };
        Add(ListOption);
        AllOption = new Option<bool>("--all") { Description = "Delete all items" };
        Add(AllOption);
        DoDeleteOption = new Option<bool>("--do-delete") { Description = "Perform actual delete" };
        Add(DoDeleteOption);
        Validators.Add(result =>
        {
            bool anyFilters = false;
            anyFilters |= result.GetValue(ToolOption) != null;
            anyFilters |= result.GetValue(GroupOption) != null;
            anyFilters |= result.GetValue(ToolLikeOption) != null;
            anyFilters |= result.GetValue(GroupLikeOption) != null;
            anyFilters |= result.GetValue(IdOption) != null;
            anyFilters |= result.GetValue(IdLikeOption) != null;
            anyFilters |= result.GetValue(NameLikeOption) != null;
            if (result.GetValue(AllOption))
            {
                if (anyFilters)
                {
                    result.AddError("Cannot specify --all when filters have been specified.");
                }
            }
            else if (!anyFilters)
            {
                result.AddError("At least one filter or --all must be specified.");
            }
        });
    }

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var arm = RegistrationProvider.CreateArtifactRegistrationManager(parseResult);
        IEnumerable<ArtifactInfo> en;
        if (parseResult.GetValue(AllOption))
        {
            en = await arm.ListArtifactsAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            string? tool = parseResult.GetValue(ToolOption);
            string? group = parseResult.GetValue(GroupOption);
            string? toolLike = parseResult.GetValue(ToolLikeOption);
            string? groupLike = parseResult.GetValue(GroupLikeOption);
            string? id = parseResult.GetValue(IdOption);
            string? idLike = parseResult.GetValue(IdLikeOption);
            string? nameLike = parseResult.GetValue(NameLikeOption);
            en = (await arm.ListArtifactsOptionalsAsync(tool, group, cancellationToken: cancellationToken).ConfigureAwait(false)).WithFilters(tool, toolLike, group, groupLike, id, idLike, nameLike);
        }
        int v = 0;
        bool list = parseResult.GetValue(ListOption);
        bool doDelete = parseResult.GetValue(DoDeleteOption);
        bool listResource = parseResult.GetValue(ListResourceOption);
        bool detailed = parseResult.GetValue(DetailedOption);
        foreach (ArtifactInfo i in en.ToList())
        {
            if (list) await Common.DisplayAsync(i, listResource, arm, detailed, ToolOutput).ConfigureAwait(false);
            if (doDelete) await arm.RemoveArtifactAsync(i.Key, cancellationToken).ConfigureAwait(false);
            v++;
        }
        ToolOutput.Out.WriteLine(doDelete ? $"Deleted {v} records." : $"{v} records would be affected.");
        return 0;
    }
}
