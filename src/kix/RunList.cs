using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art;
using Art.Common;
using Art.Common.Proxies;

namespace Kix;

internal class RunList : ToolCommand
{
    protected Option<string> ProfileFileOption;

    protected Option<bool> ListResourceOption;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    protected Option<bool> DetailedOption;

    public RunList() : this("list", "Execute artifact list tools.")
    {
    }

    public RunList(string name, string? description = null) : base(name, description)
    {
        ProfileFileOption = new Option<string>(new[] { "-i", "--input" }, "Profile file.");
        ProfileFileOption.ArgumentHelpName = "file";
        AddOption(ProfileFileOption);
        ListResourceOption = new Option<bool>(new[] { "-l", "--list-resource" }, "List associated resources.");
        AddOption(ListResourceOption);
        ProfileFileOption.ArgumentHelpName = "file";
        AddOption(ProfileFileOption);
        ToolOption = new Option<string>(new[] { "-t", "--tool" }, "Tool to use or filter profiles by.");
        ToolOption.ArgumentHelpName = "name";
        AddOption(ToolOption);
        GroupOption = new Option<string>(new[] { "-g", "--group" }, "Group to use or filter profiles by.");
        GroupOption.ArgumentHelpName = "name";
        AddOption(GroupOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries.");
        AddOption(DetailedOption);
        AddValidator(v =>
        {
            if (v.GetValueForOption(ProfileFileOption) == null && v.GetValueForOption(ToolOption) == null)
            {
                v.ErrorMessage = $"At least one of {ProfileFileOption.Aliases.First()} or {ToolOption.Aliases.First()} must be passed";
            }
        });
        this.SetHandler(RunAsync);
    }

    public async Task<int> RunAsync(InvocationContext context)
    {
        string? profileFile = context.ParseResult.HasOption(ProfileFileOption) ? context.ParseResult.GetValueForOption(ProfileFileOption) : null;
        string? tool = context.ParseResult.HasOption(ToolOption) ? context.ParseResult.GetValueForOption(ToolOption) : null;
        string? group = context.ParseResult.HasOption(GroupOption) ? context.ParseResult.GetValueForOption(GroupOption) : null;
        if (profileFile == null) return await ExecAsync(context, new ArtifactToolProfile(tool!, group ?? "", null));
        int ec = 0;
        foreach (ArtifactToolProfile profile in ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile, JsonOpt.Options))
        {
            if (group != null && group != profile.Group || tool != null && tool != profile.Tool) continue;
            ec = Math.Max(await ExecAsync(context, profile), ec);
        }
        return ec;
    }

    private async Task<int> ExecAsync(InvocationContext context, ArtifactToolProfile profile)
    {
        IArtifactTool t;
        try
        {
            t = await GetSearchingToolAsync(context, profile);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
            return 69;
        }
        using var tool = t;
        ArtifactToolListOptions options = new();
        ArtifactToolListProxy proxy = new(tool, options, Common.GetDefaultToolLogHandler());
        bool listResource = context.ParseResult.GetValueForOption(ListResourceOption);
        bool detailed = context.ParseResult.GetValueForOption(DetailedOption);
        await foreach (ArtifactData data in proxy.ListAsync())
            if (listResource)
                await Common.DisplayAsync(data.Info, data.Values, detailed);
            else
                Common.Display(data.Info, detailed);
        return 0;
    }
}
