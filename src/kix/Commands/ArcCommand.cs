﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art;
using Art.Common;
using Art.Common.Management;
using Art.Common.Proxies;
using Art.EF.Sqlite;

namespace kix.Commands;

internal class ArcCommand : ToolCommandBase
{
    protected Option<string> DatabaseOption;

    protected Option<string> OutputOption;

    protected Option<string> HashOption;

    protected Argument<List<string>> ProfileFilesArg;

    protected Option<ResourceUpdateMode> UpdateOption;

    protected Option<bool> FullOption;

    protected Option<ArtifactSkipMode> SkipOption;

    protected Option<bool> FastExitOption;

    protected Option<bool> NullOutputOption;

    public ArcCommand() : this("arc", "Execute archival artifact tools.")
    {
    }

    public ArcCommand(string name, string? description = null) : base(name, description)
    {
        DatabaseOption = new Option<string>(new[] { "-d", "--database" }, "Sqlite database file.") { ArgumentHelpName = "file" };
        DatabaseOption.SetDefaultValue(Common.DefaultDbFile);
        AddOption(DatabaseOption);
        OutputOption = new Option<string>(new[] { "-o", "--output" }, "Output directory.") { ArgumentHelpName = "directory" };
        OutputOption.SetDefaultValue(Directory.GetCurrentDirectory());
        AddOption(OutputOption);
        HashOption = new Option<string>(new[] { "-h", "--hash" }, $"Checksum algorithm ({Common.ChecksumAlgorithms}).");
        HashOption.SetDefaultValue("SHA256");
        AddOption(HashOption);
        ProfileFilesArg = new Argument<List<string>>("profile", "Profile file(s).") { HelpName = "file", Arity = ArgumentArity.OneOrMore };
        AddArgument(ProfileFilesArg);
        UpdateOption = new Option<ResourceUpdateMode>(new[] { "-u", "--update" }, $"Resource update mode ({Common.ResourceUpdateModes}).") { ArgumentHelpName = "mode" };
        UpdateOption.SetDefaultValue(ResourceUpdateMode.ArtifactHard);
        AddOption(UpdateOption);
        FullOption = new Option<bool>(new[] { "-f", "--full" }, "Only process full artifacts.");
        AddOption(FullOption);
        SkipOption = new Option<ArtifactSkipMode>(new[] { "-s", "--skip" }, $"Skip artifacts ({Common.ArtifactSkipModes}).");
        SkipOption.ArgumentHelpName = "mode";
        SkipOption.SetDefaultValue(ArtifactSkipMode.None);
        AddOption(SkipOption);
        FastExitOption = new Option<bool>(new[] { "-z", "--fast-exit" }, $"Equivalent to -s/--skip {nameof(ArtifactSkipMode.FastExit)}.");
        AddOption(FastExitOption);
        NullOutputOption = new Option<bool>(new[] { "--null-output" }, "Send resources to the void.");
        AddOption(NullOutputOption);
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        string? hash = context.ParseResult.HasOption(HashOption) ? context.ParseResult.GetValueForOption(HashOption) : null;
        hash = string.Equals(hash, "none", StringComparison.InvariantCultureIgnoreCase) ? null : hash;
        if (hash != null && !ChecksumSource.DefaultSources.ContainsKey(hash))
        {
            Console.WriteLine($"Failed to find hash algorithm {hash}\nKnown algorithms:");
            foreach (string id in ChecksumSource.DefaultSources.Values.Select(v => v.Id))
                Console.WriteLine(id);
            return 2;
        }
        ResourceUpdateMode update = context.ParseResult.GetValueForOption(UpdateOption);
        bool full = context.ParseResult.GetValueForOption(FullOption);
        ArtifactSkipMode skip = context.ParseResult.GetValueForOption(SkipOption);
        bool fastExit = context.ParseResult.GetValueForOption(FastExitOption);
        bool nullOutput = context.ParseResult.GetValueForOption(NullOutputOption);
        ArtifactToolDumpOptions options = new(update, !full, fastExit ? ArtifactSkipMode.FastExit : skip, hash);
        IArtifactDataManager adm = nullOutput ? new NullArtifactDataManager() : new DiskArtifactDataManager(context.ParseResult.GetValueForOption(OutputOption)!);
        using SqliteArtifactRegistrationManager arm = new(context.ParseResult.GetValueForOption(DatabaseOption)!);
        IToolLogHandler l = Common.GetDefaultToolLogHandler();
        List<ArtifactToolProfile> profiles = new();
        foreach (string profileFile in context.ParseResult.GetValueForArgument(ProfileFilesArg))
            profiles.AddRange(ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile, JsonOpt.Options));
        string? cookieFile = context.ParseResult.HasOption(CookieFileOption) ? context.ParseResult.GetValueForOption(CookieFileOption) : null;
        IEnumerable<string> properties = context.ParseResult.HasOption(PropertiesOption) ? context.ParseResult.GetValueForOption(PropertiesOption)! : Array.Empty<string>();
        profiles = profiles.Select(p => p.GetWithConsoleOptions(cookieFile, properties)).ToList();
        foreach (ArtifactToolProfile profile in profiles)
        {
            Plugin plugin = Plugin.LoadForToolString(profile.Tool);
            await ArtifactDumping.DumpAsync(plugin.Context, profile, arm, adm, options, l).ConfigureAwait(false);
        }
        return 0;
    }
}