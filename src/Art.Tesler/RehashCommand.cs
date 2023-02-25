using System.CommandLine;
using System.CommandLine.Invocation;
using System.Security.Cryptography;
using Art.Common;
using Art.Common.Management;
using Art.EF.Sqlite;

namespace Art.Tesler;

internal class RehashCommand : CommandBase
{
    protected Option<string> DatabaseOption;

    protected Option<string> OutputOption;

    protected Option<string> HashOption;

    protected Option<bool> DetailedOption;

    public RehashCommand() : this("rehash", "Recompute hashes for archive contents.")
    {
    }

    public RehashCommand(string name, string? description = null) : base(name, description)
    {
        DatabaseOption = new Option<string>(new[] { "-d", "--database" }, "Sqlite database file") { ArgumentHelpName = "file" };
        DatabaseOption.SetDefaultValue(Common.DefaultDbFile);
        AddOption(DatabaseOption);
        OutputOption = new Option<string>(new[] { "-o", "--output" }, "Output directory") { ArgumentHelpName = "directory" };
        OutputOption.SetDefaultValue(Directory.GetCurrentDirectory());
        AddOption(OutputOption);
        HashOption = new Option<string>(new[] { "-h", "--hash" }, $"Checksum algorithm ({Common.ChecksumAlgorithms})") { IsRequired = true };
        AddOption(HashOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries");
        AddOption(DetailedOption);
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        string hash = context.ParseResult.GetValueForOption(HashOption)!;
        if (!ChecksumSource.DefaultSources.ContainsKey(hash))
        {
            PrintErrorMessage(Common.GetInvalidHashMessage(hash));
            return 2;
        }
        ArtifactDataManager adm = new DiskArtifactDataManager(context.ParseResult.GetValueForOption(OutputOption)!);
        using SqliteArtifactRegistrationManager arm = new(context.ParseResult.GetValueForOption(DatabaseOption)!);
        Dictionary<ArtifactKey, List<ArtifactResourceInfo>> failed = new();
        int rehashed = 0;

        void AddFail(ArtifactResourceInfo r)
        {
            if (!failed.TryGetValue(r.Key.Artifact, out var list)) list = failed[r.Key.Artifact] = new List<ArtifactResourceInfo>();
            list.Add(r);
        }

        bool detailed = context.ParseResult.GetValueForOption(DetailedOption);
        foreach (ArtifactInfo inf in await arm.ListArtifactsAsync())
        foreach (ArtifactResourceInfo rInf in await arm.ListResourcesAsync(inf.Key))
        {
            if (rInf.Checksum == null || !ChecksumSource.DefaultSources.TryGetValue(rInf.Checksum.Id, out ChecksumSource? haOriginalV))
                continue;
            using HashAlgorithm haOriginal = haOriginalV.HashAlgorithmFunc!();
            if (!await adm.ExistsAsync(rInf.Key))
            {
                AddFail(rInf);
                continue;
            }
            if (!ChecksumSource.DefaultSources.TryGetValue(hash, out ChecksumSource? haNewV))
            {
                PrintErrorMessage($"Failed to instantiate new hash algorithm for {hash}");
                return 2;
            }
            Common.PrintFormat(rInf.GetInfoPathString(), detailed, () => rInf.GetInfoString());
            using HashAlgorithm haNew = haNewV.HashAlgorithmFunc!();
            await using Stream sourceStream = await adm.OpenInputStreamAsync(rInf.Key);
            await using HashProxyStream hpsOriginal = new(sourceStream, haOriginal, true, true);
            await using HashProxyStream hpsNew = new(hpsOriginal, haNew, true, true);
            await using MemoryStream ms = new();
            await hpsNew.CopyToAsync(ms);
            if (!rInf.Checksum.Value.AsSpan().SequenceEqual(hpsOriginal.GetHash()))
            {
                AddFail(rInf);
                continue;
            }
            ArtifactResourceInfo nInf = rInf with { Checksum = new Checksum(haNewV.Id, hpsNew.GetHash()) };
            await arm.AddResourceAsync(nInf);
            Common.PrintFormat(nInf.GetInfoPathString(), detailed, () => nInf.GetInfoString());
            rehashed++;
        }
        Console.WriteLine();
        if (failed.Count != 0)
        {
            PrintErrorMessage($"{failed.Sum(v => v.Value.Count)} resources with checksums failed validation before rehash.");
            foreach (ArtifactResourceInfo value in failed.Values.SelectMany(v => v)) Common.Display(value, detailed);
            return 1;
        }
        Console.WriteLine($"{rehashed} resources successfully rehashed.");
        return 0;
    }
}
