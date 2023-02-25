using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art.Common;
using Art.Common.Management;
using Art.Common.Proxies;
using Art.EF.Sqlite;

namespace Art.Tesler;

internal class DumpCommand<TPluginStore> : ToolCommandBase<TPluginStore> where TPluginStore : IRegistryStore
{
    protected Option<string> DatabaseOption;

    protected Option<string> OutputOption;

    protected Option<string> HashOption;

    protected Option<bool> NoDatabaseOption;

    protected Option<string> ProfileFileOption;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    public DumpCommand(TPluginStore pluginStore) : this(pluginStore, "dump", "Execute artifact dump tools.")
    {
    }

    public DumpCommand(TPluginStore pluginStore, string name, string? description = null) : base(pluginStore, name, description)
    {
        DatabaseOption = new Option<string>(new[] { "-d", "--database" }, "Sqlite database file") { ArgumentHelpName = "file" };
        DatabaseOption.SetDefaultValue(Common.DefaultDbFile);
        AddOption(DatabaseOption);
        OutputOption = new Option<string>(new[] { "-o", "--output" }, "Output directory") { ArgumentHelpName = "directory" };
        OutputOption.SetDefaultValue(Directory.GetCurrentDirectory());
        AddOption(OutputOption);
        HashOption = new Option<string>(new[] { "-h", "--hash" }, $"Checksum algorithm ({Common.ChecksumAlgorithms})");
        HashOption.SetDefaultValue(Common.DefaultChecksumAlgorithm);
        AddOption(HashOption);
        NoDatabaseOption = new Option<bool>(new[] { "--no-database" }, "Don't use database to track resources");
        AddOption(NoDatabaseOption);
        ProfileFileOption = new Option<string>(new[] { "-i", "--input" }, "Profile file") { ArgumentHelpName = "file" };
        AddOption(ProfileFileOption);
        ToolOption = new Option<string>(new[] { "-t", "--tool" }, "Tool to use or filter profiles by") { ArgumentHelpName = "name" };
        AddOption(ToolOption);
        GroupOption = new Option<string>(new[] { "-g", "--group" }, "Group to use or filter profiles by") { ArgumentHelpName = "name" };
        AddOption(GroupOption);
        AddValidator(v =>
        {
            if (v.GetValueForOption(ProfileFileOption) == null && v.GetValueForOption(ToolOption) == null)
            {
                v.ErrorMessage = $"At least one of {ProfileFileOption.Aliases.First()} or {ToolOption.Aliases.First()} must be passed.";
            }
        });
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        ArtifactDataManager adm = new DiskArtifactDataManager(context.ParseResult.GetValueForOption(OutputOption)!);
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
        string? hash = context.ParseResult.HasOption(HashOption) ? context.ParseResult.GetValueForOption(HashOption) : null;
        hash = string.Equals(hash, "none", StringComparison.InvariantCultureIgnoreCase) ? null : hash;
        if (hash != null && !ChecksumSource.DefaultSources.ContainsKey(hash))
        {
            PrintErrorMessage(Common.GetInvalidHashMessage(hash));
            return 2;
        }
        string? profileFile = context.ParseResult.HasOption(ProfileFileOption) ? context.ParseResult.GetValueForOption(ProfileFileOption) : null;
        string? tool = context.ParseResult.HasOption(ToolOption) ? context.ParseResult.GetValueForOption(ToolOption) : null;
        string? group = context.ParseResult.HasOption(GroupOption) ? context.ParseResult.GetValueForOption(GroupOption) : null;
        if (profileFile == null) return await ExecAsync(context, new ArtifactToolProfile(tool!, group ?? "default", null), arm, adm, hash);
        int ec = 0;
        foreach (ArtifactToolProfile profile in ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile))
        {
            if (group != null && group != profile.Group || tool != null && tool != profile.Tool) continue;
            ec = Common.AccumulateErrorCode(await ExecAsync(context, profile, arm, adm, hash), ec);
        }
        return ec;
    }

    private async Task<int> ExecAsync(InvocationContext context, ArtifactToolProfile profile, IArtifactRegistrationManager arm, ArtifactDataManager adm, string? hash)
    {
        ArtifactToolDumpOptions options = new(ChecksumId: hash);
        using var tool = await GetToolAsync(context, profile, arm, adm);
        ArtifactToolDumpProxy dProxy = new(tool, options, Common.GetDefaultToolLogHandler());
        await dProxy.DumpAsync();
        return 0;
    }
}
