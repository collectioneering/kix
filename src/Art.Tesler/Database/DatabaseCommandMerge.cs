using System.CommandLine;
using Art.Common;

namespace Art.Tesler.Database;

public class DatabaseCommandMerge : DatabaseCommandBase
{
    public enum MergeFilter
    {
        /// <summary>
        /// Only merge artifacts that are new or are full when previous was non-full.
        /// </summary>
        New,

        /// <summary>
        /// Only merge artifacts considered updated.
        /// </summary>
        Updated,

        /// <summary>
        /// Allow merging all selected artifacts.
        /// </summary>
        ForceAll
    }

    protected ITeslerRegistrationProvider InputRegistrationProvider;

    protected Option<bool> ListOption;

    protected Option<bool> AllOption;

    protected Option<bool> DoMergeOption;

    protected Option<MergeFilter> MergeFilterOption;

    public DatabaseCommandMerge(
        IOutputControl toolOutput,
        ITeslerRegistrationProvider registrationProvider,
        ITeslerRegistrationProvider inputRegistrationProvider,
        string name,
        string? description = null)
        : base(toolOutput, registrationProvider, name, description)
    {
        InputRegistrationProvider = inputRegistrationProvider;
        InputRegistrationProvider.Initialize(this);
        ListOption = new Option<bool>("--list") { Description = "List items" };
        Add(ListOption);
        AllOption = new Option<bool>("--all") { Description = "Delete all items" };
        Add(AllOption);
        DoMergeOption = new Option<bool>("--do-merge") { Description = "Perform actual merge" };
        Add(DoMergeOption);
        MergeFilterOption = new Option<MergeFilter>("--merge-filter") { Description = "Filter artifacts to commit" };
        MergeFilterOption.DefaultValueFactory = static _ => MergeFilter.Updated;
        Add(MergeFilterOption);
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
        using var inputArm = InputRegistrationProvider.CreateArtifactRegistrationManager(parseResult);
        IEnumerable<ArtifactInfo> en;
        if (parseResult.GetValue(AllOption))
        {
            en = await inputArm.ListArtifactsAsync(cancellationToken).ConfigureAwait(false);
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
            en = (await inputArm.ListArtifactsOptionalsAsync(tool, group, cancellationToken: cancellationToken).ConfigureAwait(false)).WithFilters(tool, toolLike, group, groupLike, id, idLike, nameLike);
        }
        MergeFilter mergeFilter = parseResult.GetValue(MergeFilterOption);
        int v = 0;
        bool list = parseResult.GetValue(ListOption);
        bool doMerge = parseResult.GetValue(DoMergeOption);
        bool listResource = parseResult.GetValue(ListResourceOption);
        bool detailed = parseResult.GetValue(DetailedOption);
        foreach (ArtifactInfo i in en.ToList())
        {
            ArtifactInfo? existing = await arm.TryGetArtifactAsync(i.Key, cancellationToken).ConfigureAwait(false);
            switch (mergeFilter)
            {
                case MergeFilter.Updated:
                    if ((ItemStateFlagsUtility.GetItemStateFlags(existing, i) & ItemStateFlags.NewerIdentityMask) == 0)
                    {
                        continue;
                    }
                    break;
                case MergeFilter.New:
                    if ((ItemStateFlagsUtility.GetItemStateFlags(existing, i) & ItemStateFlags.New) == 0)
                    {
                        continue;
                    }
                    break;
                case MergeFilter.ForceAll:
                    break;
                default:
                    throw new ArgumentException($"Invalid merge filter value {mergeFilter}");
            }
            if (list)
            {
                await Common.DisplayAsync(i, listResource, inputArm, detailed, ToolOutput).ConfigureAwait(false);
                if (existing != null)
                {
                    await Common.DisplayAsync(existing, listResource, arm, detailed, ToolOutput).ConfigureAwait(false);
                }
            }
            if (doMerge)
            {
                if (existing != null)
                {
                    await arm.RemoveArtifactAsync(i.Key, cancellationToken).ConfigureAwait(false);
                }
                await arm.AddArtifactAsync(i, cancellationToken).ConfigureAwait(false);
                foreach (var resource in await inputArm.ListResourcesAsync(i.Key, cancellationToken).ConfigureAwait(false))
                {
                    await arm.AddResourceAsync(resource, cancellationToken).ConfigureAwait(false);
                }
            }
            v++;
        }
        ToolOutput.Out.WriteLine(doMerge ? $"Merged {v} records." : $"{v} records would be affected.");
        return 0;
    }
}
