using System.Collections.Immutable;
using System.CommandLine;
using Art.Common;
using Art.Common.Modular;
using Art.Tesler;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

var cfg = new ModuleLoadConfiguration(new[] { "Art" }.ToImmutableHashSet());
var providers = new List<IModuleProvider>();
string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
providers.AddRange(await ModuleSearchConfigurationUtility.GetModuleProvidersByPathsAsync(
    cfg,
    baseDirectory,
    Directory.GetFiles(baseDirectory, "*.kix_search_config.json")
));
providers.Add(ModuleManifestProvider.Create(cfg, Path.Combine(baseDirectory, "Plugins"), ".kix", ".kix.json"));
var provider = new AggregateModuleProvider([..providers]);
var registryStore = new ModularArtifactToolRegistryStore(provider);
var toolLogHandlerProvider = new ConsoleStyledToolLogHandlerProvider(
    Console.Out,
    Console.Error,
    Console.Error,
    static () => Console.IsErrorRedirected,
    static () => Console.BufferWidth,
    static () => Console.WindowHeight,
    static () => Console.CursorTop - Console.WindowTop,
    Console.OpenStandardOutput);
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

var dataProvider = new DiskTeslerDataProvider();
var registrationProvider = new SqliteTeslerRegistrationProvider();
var inputRegistrationProvider = new SqliteTeslerRegistrationProvider(new Option<string>("--input-database") { HelpName = "file", Required = true, Description = "Sqlite database file (input)" });
var diskProfileResolver = new DiskProfileResolver();
var selectableToolProfileResolver = new SelectableToolProfileResolver(registryStore);
var profileResolver = new AggregateProfileResolver([diskProfileResolver, selectableToolProfileResolver]);
var rootCommand = TeslerRootCommand.Create(
    toolLogHandlerProvider,
    registryStore,
    runnerPropProvider,
    toolPropProvider,
    dataProvider,
    registrationProvider,
    inputRegistrationProvider,
    TimeProvider.System,
    profileResolver);
var parseResult = rootCommand.Parse(args);
parseResult.InvocationConfiguration.Output = Console.Error;
parseResult.InvocationConfiguration.Error = Console.Error;
return await parseResult.InvokeAsync();
