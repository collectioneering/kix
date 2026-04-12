using System.Diagnostics.CodeAnalysis;
using Art.Common;
using Art.Common.Resources;
using Art.Tesler.Properties;
using Art.TestsBase;
using Microsoft.Extensions.Time.Testing;

namespace Art.Tesler.Tests;

public class ListCommandTests : CommandTestBase
{
    protected ListCommand? Command;

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore artifactToolRegistryStore,
        IToolPropertyProvider toolPropertyProvider,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider)
    {
        Command = new ListCommand(toolLogHandlerProvider, artifactToolRegistryStore, toolPropertyProvider, extensionsContext, timeProvider);
    }

    [Fact]
    public void EmptyInvocation_Fails()
    {
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(_ => []));
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
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(_ => []));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string[] line = ["-t", new ArtifactToolID("NOT_AN_ASSEMBLY", "MALO").GetToolString()];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.Empty(OutQueue);
        Assert.NotEmpty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.NotEqual(0, rc);
    }

    [Fact]
    public void NoResults_Success()
    {
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(_ => []));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string[] line = ["-t", ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactListTool>()];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.Empty(OutQueue);
        Assert.Empty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.Equal(0, rc);
    }

    [Fact]
    public void OneResult_Success()
    {
        const string group = "GROUP_1";
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(v =>
        {
            var results = new List<IArtifactData>();
            var data = v.CreateData("ID_1");
            data.String("RES_1_CONTENT", "RES_1").Commit();
            results.Add(data);
            return results;
        }));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactListTool>();
        string[] line = ["-t", toolString, "-g", group];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.Single(OutQueue);
        Assert.Empty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.Equal(0, rc);
        var vq = OutQueue.Dequeue();
        var vqObject = Assert.IsType<ArtifactDataObjectLog>(vq);
        var data = vqObject.ArtifactData;
        var key = data.Info.Key;
        Assert.Equal("ID_1", key.Id);
        Assert.Equal(toolString, key.Tool);
        Assert.Equal(group, key.Group);
        var rkey1 = new ArtifactResourceKey(key, "RES_1");
        Assert.Equal([rkey1], data.Keys);
        var stringArtifactResourceInfo = Assert.IsType<StringArtifactResourceInfo>(data[rkey1]);
        Assert.Equal("RES_1_CONTENT", stringArtifactResourceInfo.Resource);
    }

    [Fact]
    public void MultiResult_Success()
    {
        const string group = "GROUP_1";
        var store = GetSingleStore(ProgrammableArtifactListTool.CreateRegistryEntry(v =>
        {
            var results = new List<IArtifactData>();
            var data1 = v.CreateData("ID_1");
            data1.String("RES_1_CONTENT", "RES_1").Commit();
            results.Add(data1);
            var data2 = v.CreateData("ID_2");
            data2.String("RES_2_CONTENT", "RES_2").Commit();
            results.Add(data2);
            return results;
        }));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        CreateObjectOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, store, toolPropertyProvider, extensionsContext, new FakeTimeProvider());
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactListTool>();
        string[] line = ["-t", toolString, "-g", group];
        int rc = InvokeCommand(Command, line, console);
        Assert.Empty(Out.ToString());
        Assert.Equal(2, OutQueue.Count);
        Assert.Empty(Error.ToString());
        Assert.Empty(ErrorQueue);
        Assert.Equal(0, rc);
        var vq1 = OutQueue.Dequeue();
        var vq1Obj = Assert.IsType<ArtifactDataObjectLog>(vq1);
        var data1 = vq1Obj.ArtifactData;
        var vq2 = OutQueue.Dequeue();
        var vq2Obj = Assert.IsType<ArtifactDataObjectLog>(vq2);
        var data2 = vq2Obj.ArtifactData;
        var key1 = data1.Info.Key;
        Assert.Equal("ID_1", key1.Id);
        Assert.Equal(toolString, key1.Tool);
        Assert.Equal(group, key1.Group);
        var rkey1 = new ArtifactResourceKey(key1, "RES_1");
        Assert.Equal([rkey1], data1.Keys);
        var stringArtifactResourceInfo1 = Assert.IsType<StringArtifactResourceInfo>(data1[rkey1]);
        Assert.Equal("RES_1_CONTENT", stringArtifactResourceInfo1.Resource);
        var key2 = data2.Info.Key;
        Assert.Equal("ID_2", key2.Id);
        Assert.Equal(toolString, key2.Tool);
        Assert.Equal(group, key2.Group);
        var rkey2 = new ArtifactResourceKey(key2, "RES_2");
        Assert.Equal([rkey2], data2.Keys);
        var stringArtifactResourceInfo2 = Assert.IsType<StringArtifactResourceInfo>(data2[rkey2]);
        Assert.Equal("RES_2_CONTENT", stringArtifactResourceInfo2.Resource);
    }
}
