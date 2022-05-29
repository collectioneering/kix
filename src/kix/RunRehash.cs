using System.Security.Cryptography;
using Art;
using Art.Crypto;
using Art.EF.Sqlite;
using Art.Management;
using Art.Resources;
using CommandLine;

namespace Kix;

[Verb("rehash", HelpText = "Recompute hashes for archive contents.")]
internal class RunRehash : IRunnable
{
    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = Common.DefaultDbFile)]
    public string Database { get; set; } = null!;

    [Option('o', "output", HelpText = "Output directory.", MetaValue = "directory")]
    public string? Output { get; set; }

    [Option('h', "hash", HelpText = "Checksum algorithm (e.g. SHA1|SHA256|SHA384|SHA512|MD5).", Required = true)]
    public string Hash { get; set; } = null!;

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

    public async Task<int> RunAsync()
    {
        if (!ChecksumSource.DefaultSources.ContainsKey(Hash))
        {
            Console.WriteLine($"Failed to find hash algorithm {Hash}\nKnown algorithms:");
            foreach (string id in ChecksumSource.DefaultSources.Values.Select(v => v.Id))
                Console.WriteLine(id);
            return 2;
        }
        string output = Output ?? Directory.GetCurrentDirectory();
        ArtifactDataManager adm = new DiskArtifactDataManager(output);
        using SqliteArtifactRegistrationManager arm = new(Database);
        Dictionary<ArtifactKey, List<ArtifactResourceInfo>> failed = new();
        int rehashed = 0;

        void AddFail(ArtifactResourceInfo r)
        {
            if (!failed.TryGetValue(r.Key.Artifact, out var list)) list = failed[r.Key.Artifact] = new List<ArtifactResourceInfo>();
            list.Add(r);
        }

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
            if (!ChecksumSource.DefaultSources.TryGetValue(Hash, out ChecksumSource? haNewV))
            {
                Console.WriteLine("Failed to instantiate new hash algorithm");
                return 2;
            }
            Common.PrintFormat(rInf.GetInfoPathString(), Detailed, () => rInf.GetInfoString());
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
            Common.PrintFormat(nInf.GetInfoPathString(), Detailed, () => nInf.GetInfoString());
            rehashed++;
        }
        Console.WriteLine();
        if (failed.Count != 0)
        {
            Console.WriteLine($"{failed.Sum(v => v.Value.Count)} resources with checksums failed validation before rehash.");
            foreach (ArtifactResourceInfo value in failed.Values.SelectMany(v => v)) Common.Display(value, Detailed);
        }
        Console.WriteLine($"{rehashed} resources successfully rehashed.");
        return 0;
    }
}
