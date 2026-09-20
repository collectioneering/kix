using System.CommandLine;
using System.CommandLine.Parsing;

namespace Art.Tesler.Database;

public abstract class DatabaseCommandBase : CommandBase
{
    protected ITeslerRegistrationProvider RegistrationProvider;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    protected Option<string> IdOption;

    protected Option<string> ToolLikeOption;

    protected Option<string> GroupLikeOption;

    protected Option<string> IdLikeOption;

    protected Option<string> NameLikeOption;

    protected Option<bool> InvertOption;

    protected Option<bool> ListResourceOption;

    protected Option<bool> DetailedOption;

    protected DatabaseCommandBase(
        IOutputControl toolOutput,
        ITeslerRegistrationProvider registrationProvider,
        string name,
        string? description = null)
        : base(toolOutput, name, description)
    {
        RegistrationProvider = registrationProvider;
        RegistrationProvider.Initialize(this);
        ToolOption = new Option<string>("-t", "--tool") { HelpName = "value", Description = "Tool to filter by" };
        Add(ToolOption);
        GroupOption = new Option<string>("-g", "--group") { HelpName = "value", Description = "Group to filter by" };
        Add(GroupOption);
        IdOption = new Option<string>("-i", "--id") { HelpName = "value", Description = "Id to filter by" };
        Add(IdOption);
        ToolLikeOption = new Option<string>("--tool-like") { HelpName = "pattern", Description = "Tool pattern to filter by" };
        Add(ToolLikeOption);
        GroupLikeOption = new Option<string>("--group-like") { HelpName = "pattern", Description = "Group pattern to filter by" };
        Add(GroupLikeOption);
        IdLikeOption = new Option<string>("--id-like") { HelpName = "pattern", Description = "Id pattern to filter by" };
        Add(IdLikeOption);
        NameLikeOption = new Option<string>("--name-like") { HelpName = "pattern", Description = "Name pattern to filter by" };
        Add(NameLikeOption);
        InvertOption = new Option<bool>("--invert") { Description = "Invert applied filter" };
        Add(InvertOption);
        ListResourceOption = new Option<bool>("-l", "--list-resource") { Description = "List resource items" };
        Add(ListResourceOption);
        DetailedOption = new Option<bool>("--detailed") { Description = "Show detailed information on entries" };
        Add(DetailedOption);
    }

    protected void ValidateDatabaseFilter(CommandResult result, Option<bool>? allOption)
    {
        bool anyFilters = false;
        anyFilters |= result.GetValue(ToolOption) != null;
        anyFilters |= result.GetValue(GroupOption) != null;
        anyFilters |= result.GetValue(ToolLikeOption) != null;
        anyFilters |= result.GetValue(GroupLikeOption) != null;
        anyFilters |= result.GetValue(IdOption) != null;
        anyFilters |= result.GetValue(IdLikeOption) != null;
        anyFilters |= result.GetValue(NameLikeOption) != null;
        bool all = allOption == null || result.GetValue(allOption);
        bool invert = result.GetValue(InvertOption);
        bool skipInvertError = false;
        if (allOption != null)
        {
            if (all)
            {
                if (anyFilters)
                {
                    result.AddError($"Cannot specify {allOption.Name} when filters have been specified.");
                }
            }
            else if (!anyFilters)
            {
                if (invert)
                {
                    result.AddError($"At least one filter must be specified with {InvertOption.Name}.");
                    skipInvertError = true;
                }
                else
                {
                    result.AddError($"At least one filter or {allOption.Name} must be specified.");
                }
            }
            if (!skipInvertError && all && invert)
            {
                result.AddError($"Cannot use {InvertOption.Name} when {allOption.Name} is specified.");
            }
        }
        else
        {
            if (!anyFilters && invert)
            {
                result.AddError($"Cannot use {InvertOption.Name} when no filters are specified.");
            }
        }
    }
}
