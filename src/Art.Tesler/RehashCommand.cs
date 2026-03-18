using System.CommandLine;
using System.Security.Cryptography;
using Art.Common;
using Art.Common.IO;

namespace Art.Tesler;

internal class RehashCommand : CommandBase
{
    protected ITeslerDataProvider DataProvider;

    protected ITeslerRegistrationProvider RegistrationProvider;

    protected Option<string> HashOption;

    protected Option<bool> DetailedOption;

    public RehashCommand(
        IOutputControl toolOutput,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider)
        : this(toolOutput, dataProvider, registrationProvider, "rehash", "Recompute hashes for archive contents.")
    {
    }

    public RehashCommand(
        IOutputControl toolOutput,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        string name,
        string? description = null)
        : base(toolOutput, name, description)
    {
        DataProvider = dataProvider;
        DataProvider.Initialize(this);
        RegistrationProvider = registrationProvider;
        RegistrationProvider.Initialize(this);
        HashOption = new Option<string>("-h", "--hash") { Required = true, Description = $"Checksum algorithm ({Common.ChecksumAlgorithms})" };
        Add(HashOption);
        DetailedOption = new Option<bool>("--detailed") { Description = "Show detailed information on entries" };
        Add(DetailedOption);
    }

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string hash = parseResult.GetRequiredValue(HashOption);
        if (!ChecksumSource.DefaultSources.ContainsKey(hash))
        {
            PrintErrorMessage(Common.GetInvalidHashMessage(hash), ToolOutput);
            return 2;
        }
        using var adm = DataProvider.CreateArtifactDataManager(parseResult);
        using var arm = RegistrationProvider.CreateArtifactRegistrationManager(parseResult);
        Dictionary<ArtifactKey, List<ArtifactResourceInfo>> failed = new();
        int rehashed = 0;

        void AddFail(ArtifactResourceInfo r)
        {
            if (!failed.TryGetValue(r.Key.Artifact, out var list)) list = failed[r.Key.Artifact] = new List<ArtifactResourceInfo>();
            list.Add(r);
        }

        bool detailed = parseResult.GetValue(DetailedOption);
        foreach (ArtifactInfo inf in await arm.ListArtifactsAsync(cancellationToken).ConfigureAwait(false))
        foreach (ArtifactResourceInfo rInf in await arm.ListResourcesAsync(inf.Key, cancellationToken).ConfigureAwait(false))
        {
            if (rInf.Checksum == null || !ChecksumSource.DefaultSources.TryGetValue(rInf.Checksum.Id, out ChecksumSource? haOriginalV))
                continue;
            using HashAlgorithm haOriginal = haOriginalV.CreateHashAlgorithm();
            if (!await adm.ExistsAsync(rInf.Key, cancellationToken).ConfigureAwait(false))
            {
                AddFail(rInf);
                continue;
            }
            if (!ChecksumSource.DefaultSources.TryGetValue(hash, out ChecksumSource? haNewV))
            {
                PrintErrorMessage($"Failed to instantiate new hash algorithm for {hash}", ToolOutput);
                return 2;
            }
            Common.PrintFormat(rInf.GetInfoPathString(), detailed, () => rInf.GetInfoString(), ToolOutput);
            using HashAlgorithm haNew = haNewV.CreateHashAlgorithm();
            Stream sourceStream = await adm.OpenInputStreamAsync(rInf.Key, cancellationToken).ConfigureAwait(false);
            await using var stream = sourceStream.ConfigureAwait(false);
            await using HashProxyStream hpsOriginal = new(sourceStream, haOriginal, true, true);
            await using HashProxyStream hpsNew = new(hpsOriginal, haNew, true, true);
            await using SinkStream ms = new();
            await hpsNew.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            if (!rInf.Checksum.Value.AsSpan().SequenceEqual(hpsOriginal.GetHash()))
            {
                AddFail(rInf);
                continue;
            }
            ArtifactResourceInfo nInf = rInf with { Checksum = new Checksum(haNewV.Id, hpsNew.GetHash()) };
            await arm.AddResourceAsync(nInf, cancellationToken).ConfigureAwait(false);
            Common.PrintFormat(nInf.GetInfoPathString(), detailed, () => nInf.GetInfoString(), ToolOutput);
            rehashed++;
        }
        ToolOutput.Out.WriteLine();
        if (failed.Count != 0)
        {
            PrintErrorMessage($"{failed.Sum(v => v.Value.Count)} resources with checksums failed validation before rehash.", ToolOutput);
            foreach (ArtifactResourceInfo value in failed.Values.SelectMany(v => v)) Common.Display(value, detailed, ToolOutput);
            return 1;
        }
        ToolOutput.Out.WriteLine($"{rehashed} resources successfully rehashed.");
        return 0;
    }
}
