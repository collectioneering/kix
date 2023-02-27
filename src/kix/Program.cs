using System.Collections.Immutable;
using System.CommandLine;
using Art.Common;
using Art.Modular;
using Art.Tesler;

var cfg = new ModuleLoadConfiguration(new[] { "Art" }.ToImmutableHashSet());
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
var provider = ModuleManifestProvider.Create(cfg, Path.Combine(baseDir, "Plugins"), ".kix", ".kix.json");
var propProvider = new DirectoryJsonDefaultPropertyProvider(baseDir, "kix_default_props.json");
return await TeslerRootCommand.Create(new ModularArtifactToolRegistryStore(provider), propProvider).InvokeAsync(args);
