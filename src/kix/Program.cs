using System.Collections.Immutable;
using System.CommandLine;
using Art.Common;
using Art.Common.Modular;
using Art.Tesler;
using Art.Tesler.Properties;

var cfg = new ModuleLoadConfiguration(new[] { "Art" }.ToImmutableHashSet());
// TODO plugin dir...
var provider = ModuleManifestProvider.Create(cfg, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"), ".kix", ".kix.json");
var registryStore = new ModularArtifactToolRegistryStore(provider);
var toolLogHandlerProvider = new ConsoleStyledToolLogHandlerProvider(Console.Out, Console.Error, Console.Error, static () => Console.IsErrorRedirected, static () => Console.BufferWidth, Console.OpenStandardOutput);
// prop dirs
const string kixDirName = ".kix";
const string kixConfigFileName = "runnerconfig-4f648146-4fbf-0346-33aa-897b930f7b23.json";
string globalDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), kixDirName);
string localDir = Path.Join(Environment.CurrentDirectory, kixDirName);
// runner props
var globalRunnerPropProvider = new FileJsonRunnerPropertyProvider(Path.Join(globalDir, kixConfigFileName));
var localRunnerPropProvider = new FileJsonRunnerPropertyProvider(Path.Join(localDir, kixConfigFileName));
var runnerPropProvider = new GlobalLocalRunnerPropertyProvider(globalRunnerPropProvider, localRunnerPropProvider);
// tool props
var globalToolPropProvider = new DirectoryJsonToolPropertyProvider(globalDir, DirectoryJsonToolPropertyProvider.DefaultFileNameTransform);
var localToolPropProvider = new DirectoryJsonToolPropertyProvider(localDir, DirectoryJsonToolPropertyProvider.DefaultFileNameTransform);
var toolPropProvider = new GlobalLocalToolPropertyProvider(globalToolPropProvider, localToolPropProvider);

var console = new ErrorOnlyConsole(Console.Error, static () => Console.IsErrorRedirected, static () => Console.IsInputRedirected, static () => Console.WindowWidth);
var dataProvider = new DiskTeslerDataProvider();
var registrationProvider = new SqliteTeslerRegistrationProvider();
var diskProfileResolver = new DiskProfileResolver();
var selectableToolProfileResolver = new SelectableToolProfileResolver(registryStore);
var profileResolver = new AggregateProfileResolver(new IProfileResolver[] { diskProfileResolver, selectableToolProfileResolver });
return await TeslerRootCommand.Create(
    toolLogHandlerProvider,
    registryStore,
    runnerPropProvider,
    toolPropProvider,
    dataProvider,
    registrationProvider,
    profileResolver).InvokeAsync(args, console);
