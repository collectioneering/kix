using System.Collections.Immutable;
using System.CommandLine;
using Art.Common;
using Art.Modular;
using Art.Tesler;

var cfg = new ModuleLoadConfiguration(new[] { "Art" }.ToImmutableHashSet());
var provider = ModuleManifestProvider.CreateDefault(cfg);
return await TeslerRootCommand.Create(new ModularArtifactToolRegistryStore(provider)).InvokeAsync(args);
