using Art;
using Art.Proxies;
using CommandLine;

namespace Kix;

[Verb("find", HelpText = "Execute artifact finder tools.")]
internal class RunFind : BRunTool, IRunnable
{
    [Value(0, HelpText = "IDs.", MetaValue = "values", MetaName = "ids")]
    public IReadOnlyCollection<string> Ids { get; set; } = null!;

    [Option('i', "input", HelpText = "Profile file.", MetaValue = "file", Group = "source")]
    public string? ProfileFile { get; set; } = null!;

    [Option('l', "list-resource", HelpText = "List associated resources.")]
    public bool ListResource { get; set; }

    [Option('t', "tool", HelpText = "Tool to use or filter profiles by.", MetaValue = "name", Group = "source")]
    public string? Tool { get; set; }


    [Option('g', "group", HelpText = "Group to use or filter profiles by.", MetaValue = "name")]
    public string? Group { get; set; }

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

    public async Task<int> RunAsync()
    {
        if (ProfileFile == null)
        {
            ArtifactToolProfile profile = new(Tool!, Group ?? "unknown", null);
            return await ExecAsync(profile);
        }
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
        ArtifactToolFindProxy proxy = new(tool);
        foreach (string id in Ids)
        {
            ArtifactData? data = null;
            try
            {
                data = await proxy.FindAsync(id);
            }
            catch (Exception ex)
            {
                if (Debug) Console.WriteLine(ex);
                continue;
            }
            finally
            {
                if (data == null) Console.WriteLine($"!! [{id}] not found");
            }
            if (data != null)
            {
                if (ListResource)
                    await Common.DisplayAsync(data.Info, data.Values, Detailed);
                else
                    Common.Display(data.Info, Detailed);
            }
        }
        return 0;
    }
}
