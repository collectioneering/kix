using System.Diagnostics.CodeAnalysis;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;
using Art.TestsBase;
using Microsoft.Extensions.Time.Testing;

namespace Art.Tesler.Tests;

public class ArcCommandTests : CommandTestBase
{
    protected ArcCommand? Command;
    private const string ProfileName = "profile";
    private const string BadProfileName = "profile_unknown";
    private static readonly ArtifactToolID s_toolId = new("Disease", "https://www.youtube.com/watch?v=GZG_HKfIz0U");
    private static readonly ArtifactToolID s_badToolId = new("BadApple", "https://www.nicovideo.jp/watch/sm8628149");

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore artifactToolRegistryStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
    {
        Command = new ArcCommand(toolLogHandlerProvider, artifactToolRegistryStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, profileResolver);
    }

    [Fact]
    public void NoProfilesPassed_Fails()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider(), profileResolver);
        Assert.NotEqual(0, InvokeCommand(Command, [], console));
        Assert.NotEmpty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
    }

    [Fact]
    public void OneItemPassed_Unmatched_Fails()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [BadProfileName];
        Assert.NotEqual(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
    }

    [Fact]
    public void OneItemPassed_AllMatch_WithNoResultingProfiles_Success()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName);
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        Assert.Equal(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void OneItemPassed_AllMatch_WithResultingProfiles_ProfilesValid_Success()
    {
        int ctr = 0;
        var store = GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(s_toolId, _ => ctr++));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile(s_toolId.GetToolString(), null, null));
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        Assert.Equal(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
        Assert.Equal(1, ctr);
    }

    [Fact]
    public void OneItemPassed_AllMatch_WithResultingProfiles_ProfilesInvalid_Fails()
    {
        int ctr = 0;
        var store = GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(s_toolId, _ => ctr++));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName, new ArtifactToolProfile(s_badToolId.GetToolString(), null, null));
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        Assert.NotEqual(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
        Assert.Equal(0, ctr);
    }
}
