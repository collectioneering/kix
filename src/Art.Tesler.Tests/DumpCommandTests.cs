using System.Diagnostics.CodeAnalysis;
using Art.Common;
using Art.Tesler.Properties;
using Art.TestsBase;
using Microsoft.Extensions.Time.Testing;

namespace Art.Tesler.Tests;

public class DumpCommandTests : CommandTestBase
{
    protected DumpCommand? Command;

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore artifactToolRegistryStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        TimeProvider timeProvider)
    {
        Command = new DumpCommand(toolLogHandlerProvider, artifactToolRegistryStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider);
    }

    [Fact]
    public void EmptyInvocation_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(_ => { }));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider());
        Assert.NotEqual(0, InvokeCommand(Command, [], console));
        Assert.NotEmpty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
    }

    [Fact]
    public void MissingTool_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(_ => { }));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider());
        string[] line = ["-t", new ArtifactToolID("NOT_AN_ASSEMBLY", "MALO").GetToolString()];
        Assert.NotEqual(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.NotEmpty(Error.ToString());
    }

    [Fact]
    public void NoopTool_Success()
    {
        var store = GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(_ => { }));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, dataProvider, registrationProvider, new FakeTimeProvider());
        string[] line = ["-t", ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactDumpTool>()];
        Assert.Equal(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
    }
}
