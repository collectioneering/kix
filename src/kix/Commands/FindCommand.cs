using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using Art;
using Art.Common.Proxies;
using Art.Modular;

namespace kix.Commands;

internal class FindCommand : ToolCommandBase
{
    protected Argument<List<string>> IdsArg;

    protected Option<string> ProfileFileOption;

    protected Option<bool> ListResourceOption;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    protected Option<bool> DetailedOption;

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public FindCommand() : this("find", "Execute artifact finder tools.")
    {
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public FindCommand(string name, string? description = null) : base(name, description)
    {
        IdsArg = new Argument<List<string>>("ids", "IDs") { HelpName = "id", Arity = ArgumentArity.OneOrMore };
        AddArgument(IdsArg);
        ListResourceOption = new Option<bool>(new[] { "-l", "--list-resource" }, "List associated resources");
        AddOption(ListResourceOption);
        ProfileFileOption = new Option<string>(new[] { "-i", "--input" }, "Profile file") { ArgumentHelpName = "file" };
        AddOption(ProfileFileOption);
        ToolOption = new Option<string>(new[] { "-t", "--tool" }, "Tool to use or filter profiles by") { ArgumentHelpName = "name" };
        AddOption(ToolOption);
        GroupOption = new Option<string>(new[] { "-g", "--group" }, "Group to use or filter profiles by") { ArgumentHelpName = "name" };
        AddOption(GroupOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries");
        AddOption(DetailedOption);
        AddValidator(v =>
        {
            if (v.GetValueForOption(ProfileFileOption) == null && v.GetValueForOption(ToolOption) == null)
            {
                v.ErrorMessage = $"At least one of {ProfileFileOption.Aliases.First()} or {ToolOption.Aliases.First()} must be passed.";
            }
        });
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    protected override async Task<int> RunAsync(InvocationContext context)
    {
        string? profileFile = context.ParseResult.HasOption(ProfileFileOption) ? context.ParseResult.GetValueForOption(ProfileFileOption) : null;
        string? tool = context.ParseResult.HasOption(ToolOption) ? context.ParseResult.GetValueForOption(ToolOption) : null;
        string? group = context.ParseResult.HasOption(GroupOption) ? context.ParseResult.GetValueForOption(GroupOption) : null;
        if (profileFile == null)
        {
            ArtifactToolProfile profile = new(tool!, group ?? "unknown", null);
            return await ExecAsync(context, profile);
        }
        int ec = 0;
        foreach (ArtifactToolProfile profile in ModularProfileUtil.DeserializeProfilesFromFile(profileFile))
        {
            if (group != null && group != profile.Group || tool != null && tool != profile.Tool) continue;
            ec = Common.AccumulateErrorCode(await ExecAsync(context, profile), ec);
        }
        return ec;
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private async Task<int> ExecAsync(InvocationContext context, ArtifactToolProfile profile)
    {
        using var tool = await GetSearchingToolAsync(context, profile);
        ArtifactToolFindProxy proxy = new(tool);
        bool listResource = context.ParseResult.GetValueForOption(ListResourceOption);
        bool detailed = context.ParseResult.GetValueForOption(DetailedOption);
        foreach (string id in context.ParseResult.GetValueForArgument(IdsArg))
        {
            IArtifactData? data = null;
            try
            {
                data = await proxy.FindAsync(id);
            }
            catch (Exception ex)
            {
                PrintExceptionMessage(ex);
                continue;
            }
            finally
            {
                if (data == null) PrintWarningMessage($"!! [{id}] not found");
            }
            if (data != null)
            {
                if (listResource)
                    await Common.DisplayAsync(data.Info, data.Values, detailed);
                else
                    Common.Display(data.Info, detailed);
            }
        }
        return 0;
    }
}
