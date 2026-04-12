using System.Diagnostics.CodeAnalysis;
using Art.Common;
using Art.Common.Logging;
using Art.Tesler.Properties;
using Art.TestsBase;
using Microsoft.Extensions.Time.Testing;

namespace Art.Tesler.Tests;

public class LoggingTests : CommandTestBase
{
    protected DumpCommand? Command;

    private const string Message = "message_here";
    private const string Group = "group_here";
    private const string OutputDelimiter = "🥔";

    [MemberNotNull(nameof(Command))]
    protected void InitCommandDefault(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore artifactToolRegistryStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider)
    {
        Command = new DumpCommand(toolLogHandlerProvider, artifactToolRegistryStore, toolPropertyProvider, dataProvider, registrationProvider, extensionsContext, timeProvider);
    }

    [Fact]
    public void Dump_LogInfoTool_OutputMatches()
    {
        CreateOutputs(out var toolOutput, out var console, OutputDelimiter);
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactDumpTool>();
        int code = Execute(toolOutput, console, t => t.LogInformation(Message), ["-t", toolString, "-g", Group]);
        Assert.Equal(0, code);
        string outputContent = Out.ToString();
        Assert.NotEmpty(outputContent);
        Assert.Equal(ConstructOutput(OutputDelimiter, toolString, Group, Message, null, LogLevel.Information), outputContent);
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void Dump_LogTitleTool_OutputMatches()
    {
        CreateOutputs(out var toolOutput, out var console, OutputDelimiter);
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactDumpTool>();
        int code = Execute(toolOutput, console, t => t.LogTitle(Message), ["-t", toolString, "-g", Group]);
        Assert.Equal(0, code);
        string outputContent = Out.ToString();
        Assert.NotEmpty(outputContent);
        Assert.Equal(ConstructOutput(OutputDelimiter, toolString, Group, Message, null, LogLevel.Title), outputContent);
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void Dump_LogEntryTool_OutputMatches()
    {
        CreateOutputs(out var toolOutput, out var console, OutputDelimiter);
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactDumpTool>();
        int code = Execute(toolOutput, console, t => t.LogEntry(Message), ["-t", toolString, "-g", Group]);
        Assert.Equal(0, code);
        string outputContent = Out.ToString();
        Assert.NotEmpty(outputContent);
        Assert.Equal(ConstructOutput(OutputDelimiter, toolString, Group, Message, null, LogLevel.Entry), outputContent);
        Assert.Empty(Error.ToString());
    }

    [Fact]
    public void Dump_LogWarningTool_OutputMatches()
    {
        CreateOutputs(out var toolOutput, out var console, OutputDelimiter);
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactDumpTool>();
        int code = Execute(toolOutput, console, t => t.LogWarning(Message), ["-t", toolString, "-g", Group]);
        Assert.Equal(0, code);
        Assert.Empty(Out.ToString());
        string warnContent = Warn.ToString();
        Assert.NotEmpty(warnContent);
        Assert.Equal(ConstructWarnOutput(OutputDelimiter, toolString, Group, Message, null, LogLevel.Warning), warnContent);
    }

    [Fact]
    public void Dump_LogErrorTool_OutputMatches()
    {
        CreateOutputs(out var toolOutput, out var console, OutputDelimiter);
        string toolString = ArtifactToolIDUtil.CreateToolString<ProgrammableArtifactDumpTool>();
        int code = Execute(toolOutput, console, t => t.LogError(Message), ["-t", toolString, "-g", Group]);
        Assert.Equal(0, code);
        Assert.Empty(Out.ToString());
        string errorContent = Error.ToString();
        Assert.NotEmpty(errorContent);
        Assert.Equal(ConstructErrorOutput(OutputDelimiter, toolString, Group, Message, null, LogLevel.Error), errorContent);
    }

    private int Execute(IToolLogHandlerProvider toolLogHandlerProvider,TestConsole testConsole, Action<ProgrammableArtifactDumpTool> action, string[] line)
    {
        var store = GetSingleStore(ProgrammableArtifactDumpTool.CreateRegistryEntry(t => action(t)));
        var toolPropertyProvider = CreateInMemoryPropertyProvider();
        var dataProvider = CreateSharedMemoryDataProvider();
        var registrationProvider = CreateSharedMemoryRegistrationProvider();
        var extensionsContext = CreateMappedExtensionsContext();
        InitCommandDefault(toolLogHandlerProvider, store, toolPropertyProvider, dataProvider, registrationProvider, extensionsContext, new FakeTimeProvider());
        return InvokeCommand(Command, line, testConsole);
    }

    private static string ConstructOutput(string outputDelimiter, string toolString, string group, string? title, string? body, LogLevel logLevel)
    {
        var expectedOutput = new StringWriter { NewLine = outputDelimiter };
        var expectedOutputHandler = new PlainLogHandler(
            expectedOutput,
            TextWriter.Null,
            TextWriter.Null,
            LogPreferences.Default,
            false);
        expectedOutputHandler.Log(toolString, group, title, body, logLevel);
        return expectedOutput.ToString();
    }

    private static string ConstructWarnOutput(string outputDelimiter, string toolString, string group, string? title, string? body, LogLevel logLevel)
    {
        var expectedWarnOutput = new StringWriter { NewLine = outputDelimiter };
        var expectedWarnOutputHandler = new PlainLogHandler(
            TextWriter.Null,
            expectedWarnOutput,
            TextWriter.Null,
            LogPreferences.Default,
            false);
        expectedWarnOutputHandler.Log(toolString, group, title, body, logLevel);
        return expectedWarnOutput.ToString();
    }

    private static string ConstructErrorOutput(string outputDelimiter, string toolString, string group, string? title, string? body, LogLevel logLevel)
    {
        var expectedErrorOutput = new StringWriter { NewLine = outputDelimiter };
        var expectedErrorOutputHandler = new PlainLogHandler(
            TextWriter.Null,
            TextWriter.Null,
            expectedErrorOutput,
            LogPreferences.Default,
            false);
        expectedErrorOutputHandler.Log(toolString, group, title, body, logLevel);
        return expectedErrorOutput.ToString();
    }
}
