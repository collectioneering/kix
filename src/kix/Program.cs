using System.CommandLine;
using Art.Modular;
using Art.Tesler;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;
using Artcore;

// dirs
string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
const string kixDirName = ".kix";
string globalKixDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), kixDirName);
string localKixDirectory = Path.Join(Environment.CurrentDirectory, kixDirName);
// plugins
const string searchConfigFilePattern = "*.kix_search_config.json";
List<string> searchConfigFiles = [];
if (new DirectoryInfo(baseDirectory) is { Exists: true } baseDirectoryForSearch)
{
    searchConfigFiles.AddRange(baseDirectoryForSearch.GetFiles(searchConfigFilePattern).Select(static v => v.FullName));
}
if (new DirectoryInfo(globalKixDirectory) is { Exists: true } globalKixDirectoryForSearch)
{
    searchConfigFiles.AddRange(globalKixDirectoryForSearch.GetFiles(searchConfigFilePattern).Select(static v => v.FullName));
}
// plugins
var registryStore = new ModularArtifactToolRegistryStore(new AggregateModuleProvider<ALCModule>(
    await ModuleSearchConfigurationUtility.GetModuleProvidersByPathsAsync(
        ModuleLoadConfiguration.Create(passthroughAssemblies: "Art"),
        baseDirectory,
        searchConfigFiles)
));
// logging
var toolLogHandlerProvider = ConsoleStyledToolLogHandlerProvider.FromSystemConsole();
// prop dirs
const string kixConfigFileName = "runnerconfig-4f648146-4fbf-0346-33aa-897b930f7b23.json";
// runner props
var globalRunnerPropProvider = new FileJsonRunnerPropertyProvider(Path.Join(globalKixDirectory, kixConfigFileName));
var localRunnerPropProvider = new FileJsonRunnerPropertyProvider(Path.Join(localKixDirectory, kixConfigFileName));
var runnerPropProvider = new GlobalLocalRunnerPropertyProvider(globalRunnerPropProvider, localRunnerPropProvider);
// tool props
var globalToolPropProvider = new DirectoryJsonToolPropertyProvider(globalKixDirectory, DirectoryJsonToolPropertyProvider.DefaultFileNameTransform);
var localToolPropProvider = new DirectoryJsonToolPropertyProvider(localKixDirectory, DirectoryJsonToolPropertyProvider.DefaultFileNameTransform);
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
