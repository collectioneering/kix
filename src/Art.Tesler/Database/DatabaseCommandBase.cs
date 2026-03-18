using System.CommandLine;

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
        ListResourceOption = new Option<bool>("-l", "--list-resource") { Description = "List resource items" };
        Add(ListResourceOption);
        DetailedOption = new Option<bool>("--detailed") { Description = "Show detailed information on entries" };
        Add(DetailedOption);
    }
}
