using System.CommandLine;
using Art;
using Art.Common;
using Art.Tesler;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;
using Artcore;

// plugins
string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var registryStore = new ModularArtifactToolRegistryStore(new AggregateModuleProvider<IArtifactToolRegistry>(
    await ModuleSearchConfigurationUtility.GetModuleProvidersByPathsAsync(
        ModuleLoadConfiguration.Create(passthroughAssemblies: "Art"),
        baseDirectory,
        Directory.GetFiles(baseDirectory, "*.kix_search_config.json"),
        Plugin.Create)
));
// logging
var toolLogHandlerProvider = ConsoleStyledToolLogHandlerProvider.FromSystemConsole();
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
// backing
var dataProvider = new DiskTeslerDataProvider();
var registrationProvider = new SqliteTeslerRegistrationProvider();
var inputRegistrationProvider = new SqliteTeslerRegistrationProvider(new Option<string>("--input-database") { HelpName = "file", Required = true, Description = "Sqlite database file (input)" });
// profile resolution
var diskProfileResolver = new DiskProfileResolver();
var selectableToolProfileResolver = new SelectableToolProfileResolver(registryStore);
var profileResolver = new AggregateProfileResolver([diskProfileResolver, selectableToolProfileResolver]);
// command
var rootCommand = TeslerRootCommand.Create(
    toolLogHandlerProvider: toolLogHandlerProvider,
    pluginStore: registryStore,
    runnerPropertyProvider: runnerPropProvider,
    toolPropertyProvider: toolPropProvider,
    dataProvider: dataProvider,
    registrationProvider: registrationProvider,
    inputRegistrationProvider: inputRegistrationProvider,
    timeProvider: TimeProvider.System,
    profileResolver: profileResolver);
var parseResult = rootCommand.Parse(args);
parseResult.InvocationConfiguration.Output = Console.Error;
parseResult.InvocationConfiguration.Error = Console.Error;
return await parseResult.InvokeAsync();
