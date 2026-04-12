using System.Diagnostics.CodeAnalysis;
using Art.Common;
using Art.Common.Resources;
using Art.Tesler.Properties;
using Art.TestsBase;
using Microsoft.Extensions.Time.Testing;

namespace Art.Tesler.Tests;

public class FindCommandTests : CommandTestBase
{
    protected FindCommand? Command;

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore artifactToolRegistryStore,
        IToolPropertyProvider toolPropertyProvider,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider)
    {
        Command = new FindCommand(toolLogHandlerProvider, artifactToolRegistryStore, toolPropertyProvider, extensionsContext, timeProvider);
    }

    [Fact]
    public void EmptyInvocation_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactFindTool.CreateRegistryEntry((_, _) => null));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        int rc = InvokeCommand(Command, [], console);
        Assert.NotEmpty(Out.ToString());
        Assert.Empty(OutQueue);
        Assert.NotEmpty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void MissingTool_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactFindTool.CreateRegistryEntry((_, _) => null));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string[] line = ["-t", new ArtifactToolID("NOT_AN_ASSEMBLY", "MALO").GetToolString()];
        int rc = InvokeCommand(Command, line, console);
        Assert.NotEmpty(Out.ToString());
        Assert.Empty(OutQueue);
        Assert.NotEmpty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void MissingArgId_Success()
    {
        var store = GetSingleStore(ProgrammableArtifactFindTool.CreateRegistryEntry((_, _) => null));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string[] line = ["-t", ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactFindTool>()];
        int rc = InvokeCommand(Command, line, console);
        Assert.NotEmpty(Out.ToString());
        Assert.Empty(OutQueue);
        Assert.NotEmpty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void NoResult_Success()
    {
        const string search = "ID_1";
        var store = GetSingleStore(ProgrammableArtifactFindTool.CreateRegistryEntry((_, _) => null));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string[] line = ["-t", ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactFindTool>(), search];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.Empty(OutQueue);
        Assert.NotEmpty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.Equal(0, rc);
    }

    [Fact]
    public void Result_Success()
    {
        const string group = "GROUP_1";
        const string search = "ID_1";
        var store = GetSingleStore(ProgrammableArtifactFindTool.CreateRegistryEntry((v, k) =>
        {
            if (search.Equals(k))
            {
                var data = v.CreateData(k);
                data.String("RES_1_CONTENT", "RES_1").Commit();
                return data;
            }
            return null;
        }));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactFindTool>();
        string[] line = ["-t", toolString, "-g", group, search];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.Single(OutQueue);
        Assert.Empty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.Equal(0, rc);
        var vq = OutQueue.Dequeue();
        Assert.IsType<ArtifactDataObjectLog>(vq);
        var data = ((ArtifactDataObjectLog)vq).ArtifactData;
        var key = data.Info.Key;
        Assert.Equal(search, key.Id);
        Assert.Equal(toolString, key.Tool);
        Assert.Equal(group, key.Group);
        var rkey1 = new ArtifactResourceKey(key, "RES_1");
        Assert.Equal([rkey1], data.Keys);
        var stringArtifactResourceInfo = Assert.IsType<StringArtifactResourceInfo>(data[rkey1]);
        Assert.Equal("RES_1_CONTENT", stringArtifactResourceInfo.Resource);
    }
}
