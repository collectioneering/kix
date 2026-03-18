using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Art.Common;
using Art.Common.IO;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler.Tests;

public class CommandTestBase
{
    protected StringWriter? Out;
    protected MemoryStream? OutStream;
    protected Queue<ObjectLog>? OutQueue;
    protected StringWriter? Warn;
    protected StringWriter? Error;
    protected Queue<ObjectLog>? ErrorQueue;
    protected IOutputControl? ToolOutput;
    protected TestConsole? TestConsole;
    protected IToolPropertyProvider? ToolPropertyProvider;
    protected ITeslerRegistrationProvider? RegistrationProvider;
    protected ITeslerDataProvider? DataProvider;
    protected IProfileResolver? ProfileResolver;

    [MemberNotNull(nameof(Out))]
    [MemberNotNull(nameof(OutStream))]
    [MemberNotNull(nameof(Warn))]
    [MemberNotNull(nameof(Error))]
    [MemberNotNull(nameof(ToolOutput))]
    [MemberNotNull(nameof(TestConsole))]
    internal void CreateStreamOutputs(out PlainToolLogHandlerProvider toolLogHandlerProvider, out TestConsole console, string? newLine = null)
    {
        Out = new StringWriter();
        OutStream = new MemoryStream();
        Warn = new StringWriter();
        Error = new StringWriter();
        if (newLine != null)
        {
            Out.NewLine = newLine;
            Warn.NewLine = newLine;
            Error.NewLine = newLine;
        }
        OutStream = new MemoryStream();
        ToolOutput = toolLogHandlerProvider = new PlainToolLogHandlerProvider(
            Out,
            Warn,
            Error,
            () => new NonDisposingStream(OutStream));
        console = TestConsole = new TestConsole(Out, Error);
    }

    [MemberNotNull(nameof(Out))]
    [MemberNotNull(nameof(Warn))]
    [MemberNotNull(nameof(Error))]
    [MemberNotNull(nameof(ToolOutput))]
    [MemberNotNull(nameof(TestConsole))]
    internal void CreateOutputs(out PlainToolLogHandlerProvider toolLogHandlerProvider, out TestConsole console, string? newLine = null)
    {
        Out = new StringWriter();
        Warn = new StringWriter();
        Error = new StringWriter();
        if (newLine != null)
        {
            Out.NewLine = newLine;
            Warn.NewLine = newLine;
            Error.NewLine = newLine;
        }
        ToolOutput = toolLogHandlerProvider = new PlainToolLogHandlerProvider(
            Out,
            Warn,
            Error,
            () => throw new NotSupportedException());
        console = TestConsole = new TestConsole(Out, Error);
    }

    [MemberNotNull(nameof(Out))]
    [MemberNotNull(nameof(OutQueue))]
    [MemberNotNull(nameof(Warn))]
    [MemberNotNull(nameof(Error))]
    [MemberNotNull(nameof(ErrorQueue))]
    [MemberNotNull(nameof(ToolOutput))]
    [MemberNotNull(nameof(TestConsole))]
    internal void CreateObjectOutputs(out ObjectToolLogHandlerProvider toolLogHandlerProvider, out TestConsole console, string? newLine = null)
    {
        Out = new StringWriter();
        Warn = new StringWriter();
        Error = new StringWriter();
        if (newLine != null)
        {
            Out.NewLine = newLine;
            Warn.NewLine = newLine;
            Error.NewLine = newLine;
        }
        OutQueue = new Queue<ObjectLog>();
        ErrorQueue = new Queue<ObjectLog>();
        ToolOutput = toolLogHandlerProvider = new ObjectToolLogHandlerProvider(
            Out,
            Warn,
            Error,
            () => throw new NotSupportedException(),
            OutQueue,
            ErrorQueue);
        console = TestConsole = new TestConsole(Out, Error);
    }

    [MemberNotNull(nameof(Warn))]
    [MemberNotNull(nameof(Error))]
    [MemberNotNull(nameof(ToolOutput))]
    [MemberNotNull(nameof(TestConsole))]
    internal void CreateOutputs(out PlainToolLogHandlerProvider toolLogHandlerProvider, out TestConsole console, Stream outStream, string? newLine = null)
    {
        var outWriter = new StreamWriter(outStream, leaveOpen: true);
        Warn = new StringWriter();
        Error = new StringWriter();
        if (newLine != null)
        {
            outWriter.NewLine = newLine;
            Warn.NewLine = newLine;
            Error.NewLine = newLine;
        }
        ToolOutput = toolLogHandlerProvider = new PlainToolLogHandlerProvider(
            outWriter,
            Warn,
            Error,
            () => new NonDisposingStream(outStream));
        console = TestConsole = new TestConsole(outWriter, Error);
    }

    private class NonDisposingStream : DelegatingStream
    {
        public NonDisposingStream(Stream innerStream) : base(innerStream)
        {
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    internal StaticArtifactToolRegistryStore GetEmptyStore() => new(new ArtifactToolRegistry());

    internal StaticArtifactToolRegistryStore GetSingleStore(ArtifactToolRegistryEntry artifactToolRegistryEntry)
    {
        var registry = new ArtifactToolRegistry();
        registry.Add(artifactToolRegistryEntry);
        return new StaticArtifactToolRegistryStore(registry);
    }

    internal StaticArtifactToolRegistryStore GetSingleStore<T>() where T : IArtifactToolFactory
    {
        var registry = new ArtifactToolRegistry();
        registry.Add<T>();
        return new StaticArtifactToolRegistryStore(registry);
    }

    internal StaticArtifactToolRegistryStore GetSingleSelectableStore<T>() where T : IArtifactToolFactory, IArtifactToolSelector<string>
    {
        var registry = new ArtifactToolRegistry();
        registry.AddSelectable<T>();
        return new StaticArtifactToolRegistryStore(registry);
    }

    [MemberNotNull(nameof(ToolPropertyProvider))]
    internal InMemoryToolPropertyProvider CreateInMemoryPropertyProvider()
    {
        var result = new InMemoryToolPropertyProvider(ImmutableDictionary<string, JsonElement>.Empty, ImmutableDictionary<ArtifactToolID, IReadOnlyDictionary<string, JsonElement>>.Empty);
        ToolPropertyProvider = result;
        return result;
    }

    [MemberNotNull(nameof(ToolPropertyProvider))]
    internal InMemoryToolPropertyProvider CreateInMemoryPropertyProvider(IReadOnlyDictionary<string, JsonElement> shared)
    {
        var result = new InMemoryToolPropertyProvider(shared, ImmutableDictionary<ArtifactToolID, IReadOnlyDictionary<string, JsonElement>>.Empty);
        ToolPropertyProvider = result;
        return result;
    }

    [MemberNotNull(nameof(ToolPropertyProvider))]
    internal InMemoryToolPropertyProvider CreateInMemoryPropertyProvider(IReadOnlyDictionary<string, JsonElement> shared, IReadOnlyDictionary<ArtifactToolID, IReadOnlyDictionary<string, JsonElement>> perTool)
    {
        var result = new InMemoryToolPropertyProvider(shared, perTool);
        ToolPropertyProvider = result;
        return result;
    }

    [MemberNotNull(nameof(DataProvider))]
    internal SharedMemoryDataProvider CreateSharedMemoryDataProvider()
    {
        var result = new SharedMemoryDataProvider();
        DataProvider = result;
        return result;
    }

    [MemberNotNull(nameof(RegistrationProvider))]
    internal SharedMemoryRegistrationProvider CreateSharedMemoryRegistrationProvider()
    {
        var result = new SharedMemoryRegistrationProvider();
        RegistrationProvider = result;
        return result;
    }

    [MemberNotNull(nameof(ProfileResolver))]
    internal DictionaryProfileResolver CreateDictionaryProfileResolver()
    {
        var result = new DictionaryProfileResolver(ImmutableDictionary<string, IReadOnlyList<ArtifactToolProfile>>.Empty);
        ProfileResolver = result;
        return result;
    }

    [MemberNotNull(nameof(ProfileResolver))]
    internal DictionaryProfileResolver CreateDictionaryProfileResolver(IReadOnlyDictionary<string, IReadOnlyList<ArtifactToolProfile>> map)
    {
        var result = new DictionaryProfileResolver(map);
        ProfileResolver = result;
        return result;
    }

    [MemberNotNull(nameof(ProfileResolver))]
    internal DictionaryProfileResolver CreateDictionaryProfileResolver(string profileName, params ArtifactToolProfile[] profiles)
    {
        return CreateDictionaryProfileResolver(new Dictionary<string, IReadOnlyList<ArtifactToolProfile>> { [profileName] = profiles });
    }

    protected static int InvokeCommand(Command command, IReadOnlyList<string> args, TestConsole testConsole)
    {
        if (command.Action is not { } action)
        {
            throw new InvalidOperationException();
        }
        var parseResult = command.Parse(args);
        parseResult.InvocationConfiguration.Output = testConsole.Out;
        parseResult.InvocationConfiguration.Error = testConsole.Error;
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                testConsole.Error.WriteLine(error.Message);
            }
            new HelpAction().Invoke(parseResult);
            return -1;
        }
        try
        {
            switch (action)
            {
                case SynchronousCommandLineAction synchronousCommandLineAction:
                    return synchronousCommandLineAction.Invoke(parseResult);
                case AsynchronousCommandLineAction asynchronousCommandLineAction:
                    return asynchronousCommandLineAction.InvokeAsync(parseResult).Result;
                default:
                    throw new ArgumentException();
            }
        }
        catch (Exception e)
        {
            testConsole.Error.WriteLine(e.ToString());
            return -2;
        }
    }
}
