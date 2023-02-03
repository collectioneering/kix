# kix

The `kix` program provides a simple way to run tools designed with the [Art](https://github.com/collectioneering/Art) library.

Artifact registration is managed by Sqlite database files, and artifact data (resources) are written to a selected folder on disk.

No tools are directly provided with this program.

## Features

- `arc`: Performs unified archival from list/dump tools by profile
- `find`/`list`/`dump`: Executes tools by tool+group or profile
- `rehash`: Recomputes resource hashes with the specified hash algorithm
- `validate`: Validates resource data based on stored hash
  - Optionally repairs resources that failed validation
- `tools`: Enumerates available plugins
- `db`: Provides limited Sqlite database management (only artifact listing and deletion at this time)

## Plugins

### File Structure

Each plugin should be placed under `Plugins/<some-name>.kix/`, e.g. `Plugins/MyToolAssembly.kix/`.

### Plugin Loading

Plugins are loaded into separate [`AssemblyLoadContext`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext)s and are primarily manipulated under the `Art` interface assembly. Tool assemblies should be compiled with the same Art interface library that `kix` is compiled with to ensure interface compatibility. Resolution of the `Art` assembly always falls through to the program's default `AssemblyLoadContext` for interface purposes, while other dependencies are resolved by an [`AssemblyDependencyResolver`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblydependencyresolver), which uses the corresponding `.deps.json` file to correctly resolve platform-specific managed and native dependencies.

### Plugin Creation

Plugins should contain one or more tools that implement one of `IArtifactTool`'s sub-interfaces, such as `IArtifactToolList`, `IArtifactToolFind`, or `IArtifactToolDump`. The base classes `ArtifactTool` (from package [Art.Common](https://www.nuget.org/packages/Art.Common)) `HttpArtifactTool` (from package [Art.Http](https://www.nuget.org/packages/Art.Http)), and `HtmlArtifactTool` (from package [Art.Html](https://www.nuget.org/packages/Art.Html)) are useful starting points.

Plugins are recognized by the presence of a `.kix.json` manifest file, which simply tells `kix` what assembly to load. For example:

```json
{
    "Assembly": "MyToolAssembly"
}
```

This file should be added to the project's outputs. For example, `MyToolAssembly.kix.json` should be given a `Content` element in the project as follows:

```xml
  <ItemGroup>
    <Content Include="MyToolAssembly.kix.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

Tool assemblies should be published (`dotnet publish`) to ensure all plugin-specific dependencies are available in the output folder.

## Pending

- User agent library to keep user agent up to date

## Related

[sqz269's BooruScraper](https://github.com/sqz269/BooruScraper) is epic for grabbing all the pics of your waifus.
