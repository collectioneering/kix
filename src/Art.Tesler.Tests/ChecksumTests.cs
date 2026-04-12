using System.Diagnostics.CodeAnalysis;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;
using Microsoft.Extensions.Time.Testing;

namespace Art.Tesler.Tests;

public class ChecksumTests : CommandTestBase
{
    private const string ProfileName = "profile";

    protected ArcCommand? Command;

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore artifactToolRegistryStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
    {
        Command = new ArcCommand(toolLogHandlerProvider, artifactToolRegistryStore, toolPropertyProvider, dataProvider, registrationProvider, extensionsContext, timeProvider, profileResolver);
    }

    [Fact]
    public void NoChecksumPassed_Success()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName);
        var extensionsContext = CreateMappedExtensionsContext();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, extensionsContext, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName];
        Assert.Equal(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void KnownChecksumPassed_Success()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName);
        var extensionsContext = CreateMappedExtensionsContext();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, extensionsContext, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName, "--hash", "SHA256"];
        Assert.Equal(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void BadChecksumPassed_Fails()
    {
        var store = GetEmptyStore();
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var profileResolver = CreateDictionaryProfileResolver(ProfileName);
        var extensionsContext = CreateMappedExtensionsContext();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, extensionsContext, new FakeTimeProvider(), profileResolver);
        string[] line = [ProfileName, "--hash", "BAD_CHECKSUM"];
        Assert.NotEqual(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
    }
}
