using System.Collections.Immutable;
using System.CommandLine;
using Art.Common;
using Art.Modular;
using Art.Tesler;

var cfg = new ModuleLoadConfiguration(new[] { "Art" }.ToImmutableHashSet());
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
var provider = ModuleManifestProvider.Create(cfg, Path.Combine(baseDir, "Plugins"), ".kix", ".kix.json");
string? defaultPropFile = Path.Combine(baseDir, "kix_default_props.json");
if (!File.Exists(defaultPropFile))
{
    defaultPropFile = null;
}
var propProvider = new DirectoryJsonDefaultPropertyProvider(baseDir, defaultPropFile);
var console = new ErrorOnlySystemConsole();
return await TeslerRootCommand.Create(new ModularArtifactToolRegistryStore(provider), propProvider).InvokeAsync(args, console);
