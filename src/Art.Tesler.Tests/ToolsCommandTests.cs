using System.Diagnostics.CodeAnalysis;
using Art.TestsBase;

namespace Art.Tesler.Tests;

public class ToolsCommandTests : CommandTestBase
{
    protected ToolsCommand? Command;

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(IOutputControl toolOutput, IArtifactToolRegistryStore artifactToolRegistryStore)
    {
        Command = new ToolsCommand(toolOutput, artifactToolRegistryStore);
    }

    [Fact]
    public void DefaultExecution_Empty_Success()
    {
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, GetEmptyStore());
        Assert.Equal(0, InvokeCommand(Command, [], console));
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void DefaultExecution_Single_Success()
    {
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(_ => { })));
        Assert.Equal(0, InvokeCommand(Command, [], console));
        Assert.Contains(nameof(ProgrammableArtifactDumpTool), Out.ToString());
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void Search_NoMatch_NotFound()
    {
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(_ => { })));
        string[] line = ["-s", "$$NOT_A_REAL_TOOL$$"];
        Assert.Equal(0, InvokeCommand(Command, line, console));
        Assert.Empty(Out.ToString());
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void Search_SingleMatching_Found()
    {
        CreateOutputs(out var toolOutput, out var console);
        InitCommandDefault(toolOutput, GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(_ => { })));
        string[] line = ["-s", nameof(ProgrammableArtifactDumpTool)];
        Assert.Equal(0, InvokeCommand(Command, line, console));
        Assert.Contains(nameof(ProgrammableArtifactDumpTool), Out.ToString());
        Assert.Empty(Error.ToString());
    }
}
