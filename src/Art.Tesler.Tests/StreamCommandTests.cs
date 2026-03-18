using System.Diagnostics.CodeAnalysis;
using Art.Common.Resources;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;
using Art.TestsBase;
using Microsoft.Extensions.Time.Testing;

namespace Art.Tesler.Tests;

public class StreamCommandTests : CommandTestBase
{
    protected StreamCommand? Command;
    private const string ProfileName = "profile";
    private const string BadProfileName = "profile_unknown";
    private static readonly ArtifactToolID s_toolId = new("TearingThroughHeaven", "https://www.youtube.com/watch?v=kef24Po3pc0");
    private static readonly ArtifactToolID s_badToolId = new("Run", "https://www.youtube.com/watch?v=mw2kKyJu9gY");

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore artifactToolRegistryStore,
        IToolPropertyProvider toolPropertyProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
    {
        Command = new StreamCommand(toolLogHandlerProvider, artifactToolRegistryStore, toolPropertyProvider, timeProvider, profileResolver);
    }

    [Fact]
    public void NoProfilesPassed_Fails()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver();
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        int rc = InvokeCommand(Command, [], console);
        Assert.NotEmpty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void ItemPassed_Unmatched_Fails()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver();
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [BadProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void ItemPassed_Matches_WithNoResultingProfiles_Fails()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName);
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void ItemPassed_Matches_WithMultipleResultingProfiles_Fails()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile("X1", "X2", null), new ArtifactToolProfile("X1", "X3", null));
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void ItemPassed_Matches_WithOneResultingProfile_ProfileValid_OneArtifact_Success()
    {
        byte[] data = new byte[128];
        Random.Shared.NextBytes(data);
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(s_toolId, v => [CommitStreamArtifact(v, new MemoryStream(data, true))]));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile(s_toolId.GetToolString(), null, null));
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
        Assert.Equal(0, rc);
        Assert.True(OutStream.TryGetBuffer(out var segment));
        Assert.True(segment.AsSpan().SequenceEqual(data));
    }

    [Fact]
    public void ItemPassed_Matches_WithOneResultingProfile_ProfileValid_BadToolType_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactFindTool.CreateRegistryEntry(s_toolId, (_, _) => null));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile(s_toolId.GetToolString(), null, null));
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void ItemPassed_Matches_WithOneResultingProfile_ProfileValid_NoArtifacts_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(s_toolId, _ => []));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile(s_toolId.GetToolString(), null, null));
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void ItemPassed_Matches_WithOneResultingProfile_ProfileValid_MultiArtifacts_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(s_toolId, v => [CommitStreamArtifact(v, new MemoryStream()), CommitStreamArtifact(v, new MemoryStream())]));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile(s_toolId.GetToolString(), null, null));
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void ItemPassed_Matches_WithOneResultingProfile_ProfileInvalid_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(s_toolId, _ => []));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile(s_badToolId.GetToolString(), null, null));
        CreateStreamOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.NotEqual(0, rc);
    }

    private static IArtifactData CommitStreamArtifact(ProgrammableArtifactListTool tool, Stream stream)
    {
        var data = tool.CreateData(Guid.NewGuid().ToString("N"));
        data.Stream(stream, Guid.NewGuid().ToString("N")).Commit(true);
        return data;
    }
}
