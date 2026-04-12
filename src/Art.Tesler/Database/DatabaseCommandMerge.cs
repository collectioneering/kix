using System.CommandLine;
using System.Text.Json;
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

    protected Option<FileInfo> RefactoringsFileOption;

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
        MergeFilterOption = new Option<MergeFilter>("--merge-filter") { Description = "Filter artifacts to commit", DefaultValueFactory = static _ => MergeFilter.Updated };
        Add(MergeFilterOption);
        RefactoringsFileOption = new Option<FileInfo>("--refactorings-file") { HelpName = "file", Description = "Use this file to apply type or assembly refactorings" };
        RefactoringsFileOption.AcceptExistingOnly();
        Add(RefactoringsFileOption);
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
        FileInfo? refactoringsFile = parseResult.GetValue(RefactoringsFileOption);
        using var arm = RegistrationProvider.CreateArtifactRegistrationManager(parseResult, isReadonly: true);
        using var inputArm = InputRegistrationProvider.CreateArtifactRegistrationManager(parseResult, isReadonly: true);
        IEnumerable<ArtifactInfo> en;
        Refactorings? refactorings = null;
        if (refactoringsFile != null)
        {
            await using (var afs = refactoringsFile.OpenRead())
            {
                refactorings = await JsonSerializer.DeserializeAsync(afs, SourceGenerationContext.s_context.Refactorings, cancellationToken);
            }
            if (refactorings == null)
            {
                throw new InvalidDataException($"Unexpected null JSON in {refactoringsFile.FullName}");
            }
        }
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
        foreach (ArtifactInfo inputArtifactInfo in en.ToList())
        {
            ArtifactKey? refactoredKey = refactorings != null && refactorings.TryGetRefactoredArtifactKey(inputArtifactInfo.Key, out var tmpRefactoredKey) ? tmpRefactoredKey : null;
            ArtifactInfo finalArtifactInfo = refactoredKey != null ? inputArtifactInfo with { Key = refactoredKey } : inputArtifactInfo;
            ArtifactInfo? existing = await arm.TryGetArtifactAsync(finalArtifactInfo.Key, cancellationToken).ConfigureAwait(false);
            switch (mergeFilter)
            {
                case MergeFilter.Updated:
                    if ((ItemStateFlagsUtility.GetItemStateFlags(existing, finalArtifactInfo) & ItemStateFlags.NewerIdentityMask) == 0)
                    {
                        continue;
                    }
                    break;
                case MergeFilter.New:
                    if ((ItemStateFlagsUtility.GetItemStateFlags(existing, finalArtifactInfo) & ItemStateFlags.New) == 0)
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
                await Common.DisplayAsync(inputArtifactInfo, listResource, inputArm, detailed, ToolOutput, refactoredKey).ConfigureAwait(false);
                if (existing != null)
                {
                    await Common.DisplayAsync(existing, listResource, arm, detailed, ToolOutput).ConfigureAwait(false);
                }
            }
            if (doMerge)
            {
                if (existing != null)
                {
                    await arm.RemoveArtifactAsync(existing.Key, cancellationToken).ConfigureAwait(false);
                }
                await arm.AddArtifactAsync(finalArtifactInfo, cancellationToken).ConfigureAwait(false);
                foreach (var resource in await inputArm.ListResourcesAsync(inputArtifactInfo.Key, cancellationToken).ConfigureAwait(false))
                {
                    await arm.AddResourceAsync(resource with { Key = resource.Key with { Artifact = finalArtifactInfo.Key } }, cancellationToken).ConfigureAwait(false);
                }
            }
            v++;
        }
        ToolOutput.Out.WriteLine(doMerge ? $"Merged {v} records." : $"{v} records would be affected.");
        return 0;
    }
}
