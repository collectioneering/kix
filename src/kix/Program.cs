using System.Collections.Immutable;
using System.CommandLine;
using Art.Common;
using Art.Common.Modular;
using Art.Tesler;

var cfg = new ModuleLoadConfiguration(new[] { "Art" }.ToImmutableHashSet());
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
string? defaultPropFile = Path.Combine(baseDir, "kix_default_props.json");
if (!File.Exists(defaultPropFile))
{
    defaultPropFile = null;
}
var provider = ModuleManifestProvider.Create(cfg, Path.Combine(baseDir, "Plugins"), ".kix", ".kix.json");
var toolLogHandlerProvider = new ConsoleStyledToolLogHandlerProvider(Console.Out, Console.Error, static () => Console.IsErrorRedirected, static () => Console.BufferWidth, Console.OpenStandardOutput);
var registryStore = new ModularArtifactToolRegistryStore(provider);
var propProvider = new DirectoryJsonDefaultPropertyProvider(baseDir, defaultPropFile);
var console = new ErrorOnlyConsole(Console.Error, static () => Console.IsErrorRedirected, static () => Console.IsInputRedirected, static () => Console.WindowWidth);
var dataProvider = new DiskTeslerDataProvider();
var registrationProvider = new SqliteTeslerRegistrationProvider();
var diskProfileResolver = new DiskProfileResolver();
var selectableToolProfileResolver = new SelectableToolProfileResolver(registryStore);
var profileResolver = new AggregateProfileResolver(new IProfileResolver[] { diskProfileResolver, selectableToolProfileResolver });
return await TeslerRootCommand.Create(toolLogHandlerProvider, registryStore, propProvider, dataProvider, registrationProvider, profileResolver).InvokeAsync(args, console);
