using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art;
using Art.Common;
using Art.Common.Management;
using Art.Common.Proxies;
using Art.EF.Sqlite;

namespace Kix;

internal class RunDump : BRunTool
{
    protected Option<string> DatabaseOption;

    protected Option<string> OutputOption;

    protected Option<bool> NoDatabaseOption;

    protected Option<string> ProfileFileOption;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    public RunDump() : this("dump", "Execute artifact dump tools.")
    {
    }

    public RunDump(string name, string? description = null) : base(name, description)
    {
        DatabaseOption = new Option<string>(new[] { "-d", "--database" }, "Sqlite database file.");
        DatabaseOption.ArgumentHelpName = "file";
        DatabaseOption.SetDefaultValue(Common.DefaultDbFile);
        AddOption(DatabaseOption);
        OutputOption = new Option<string>(new[] { "-o", "--output" }, "Output directory.");
        OutputOption.ArgumentHelpName = "directory";
        NoDatabaseOption = new Option<bool>(new[] { "--no-database" }, "Don't use database to track resources.");
        AddOption(NoDatabaseOption);
        AddOption(OutputOption);
        ProfileFileOption = new Option<string>(new[] { "-i", "--input" }, "Profile file.");
        ProfileFileOption.ArgumentHelpName = "file";
        AddOption(ProfileFileOption);
        ToolOption = new Option<string>(new[] { "-t", "--tool" }, "Tool to use or filter profiles by.");
        ToolOption.ArgumentHelpName = "name";
        AddOption(ToolOption);
        GroupOption = new Option<string>(new[] { "-g", "--group" }, "Group to use or filter profiles by.");
        GroupOption.ArgumentHelpName = "name";
        AddOption(GroupOption);
        AddValidator(v =>
        {
            if (v.GetValueForOption(ProfileFileOption) == null && v.GetValueForOption(ToolOption) == null)
            {
                v.ErrorMessage = $"At least one of {ProfileFileOption.Aliases.First()} or {ToolOption.Aliases.First()} must be passed";
            }
        });
        this.SetHandler(RunAsync);
    }

    private async Task<int> RunAsync(InvocationContext context)
    {
        string output = (context.ParseResult.HasOption(OutputOption) ? context.ParseResult.GetValueForOption(OutputOption) : null) ?? Directory.GetCurrentDirectory();
        ArtifactDataManager adm = new DiskArtifactDataManager(output);
        if (context.ParseResult.GetValueForOption(NoDatabaseOption))
        {
            InMemoryArtifactRegistrationManager arm = new();
            return await RunAsync(context, adm, arm);
        }
        else
        {
            using SqliteArtifactRegistrationManager arm = new(context.ParseResult.GetValueForOption(DatabaseOption)!);
            return await RunAsync(context, adm, arm);
        }
    }

    private async Task<int> RunAsync(InvocationContext context, ArtifactDataManager adm, IArtifactRegistrationManager arm)
    {
        string? profileFile = context.ParseResult.HasOption(ProfileFileOption) ? context.ParseResult.GetValueForOption(ProfileFileOption) : null;
        string? tool = context.ParseResult.HasOption(ToolOption) ? context.ParseResult.GetValueForOption(ToolOption) : null;
        string? group = context.ParseResult.HasOption(GroupOption) ? context.ParseResult.GetValueForOption(GroupOption) : null;
        if (profileFile == null) return await ExecAsync(context, new ArtifactToolProfile(tool!, group ?? "default", null), arm, adm);
        int ec = 0;
        foreach (ArtifactToolProfile profile in ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile, JsonOpt.Options))
        {
            if (group != null && group != profile.Group || tool != null && tool != profile.Tool) continue;
            ec = Math.Max(await ExecAsync(context, profile, arm, adm), ec);
        }
        return ec;
    }

    private async Task<int> ExecAsync(InvocationContext context, ArtifactToolProfile profile, IArtifactRegistrationManager arm, ArtifactDataManager adm)
    {
        ArtifactToolDumpOptions options = new();
        IArtifactTool t;
        try
        {
            t = await GetToolAsync(context, profile, arm, adm);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
            return 69;
        }
        using var tool = t;
        ArtifactToolDumpProxy dProxy = new(tool, options, Common.GetDefaultToolLogHandler());
        await dProxy.DumpAsync();
        return 0;
    }
}
