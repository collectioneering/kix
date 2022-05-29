using Art;
using Art.Logging;
using Art.Proxies;
using CommandLine;

namespace Kix;

[Verb("list", HelpText = "Execute artifact list tools.")]
internal class RunList : BRunTool, IRunnable
{
    [Option('i', "input", HelpText = "Profile file.", MetaValue = "file", Group = "source")]
    public string? ProfileFile { get; set; } = null!;

    [Option('l', "list-resource", HelpText = "List associated resources.")]
    public bool ListResource { get; set; }

    [Option('t', "tool", HelpText = "Tool to use or filter profiles by.", MetaValue = "name", Group = "source")]
    public string? Tool { get; set; }

    [Option('g', "group", HelpText = "Group to use or filter profiles by.", MetaValue = "name")]
    public string? Group { get; set; } = null!;

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

    public async Task<int> RunAsync()
    {
        if (ProfileFile == null) return await ExecAsync(new ArtifactToolProfile(Tool!, Group ?? "", null));
        int ec = 0;
        foreach (ArtifactToolProfile profile in ArtifactToolProfile.DeserializeProfilesFromFile(ProfileFile, JsonOpt.Options))
        {
            if (Group != null && Group != profile.Group || Tool != null && Tool != profile.Tool) continue;
            ec = Math.Max(await ExecAsync(profile), ec);
        }
        return ec;
    }

    private async Task<int> ExecAsync(ArtifactToolProfile profile)
    {
        ArtifactTool t;
        try
        {
            t = await GetSearchingToolAsync(profile);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
            return 69;
        }
        using var tool = t;
        ArtifactToolListOptions options = new();
        ArtifactToolListProxy proxy = new(tool, options, Common.GetDefaultToolLogHandler());
        await foreach (ArtifactData data in proxy.ListAsync())
            if (ListResource)
                await Common.DisplayAsync(data.Info, data.Values, Detailed);
            else
                Common.Display(data.Info, Detailed);
        return 0;
    }
}
