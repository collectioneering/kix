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

It is recommended to create an `IArtifactToolFind` tool as a base type when applicable, and mark it with `CoreAttribute`. This lets different derived tools - such as an `IArtifactToolList` that performs queries and another `IArtifactToolList` that lists items based on author - create artifacts keyed by the base type (at least by default for `ArtifactTool` subtypes). This facilitates unified management, updating, and repairing of artifacts.

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

## Tool strings

Specifying a tool to execute follows the format `<assembly-name>::<fully-qualified-type-name>`

For example, a tool `MyAssembly.MyTool` in assembly `MyAssembly` would be referenced with `MyAssembly::MyAssembly.MyTool`.

## Profiles

Profiles specify reusable configurations for tool execution. `kix` uses JSON format files to ingest profiles to prepare tools for execution or to filter content by. Each profile file may contain a single JSON object for one profile or an array of JSON objects for multiple profiles.

Example:

```json
{
    "tool": "MyAssembly::MyAssembly.MyTool",
    "group": "GroupName",
    "options": {
        "optionKey": "optionValue"
    }
}
```

- `tool`: Tool string
- `group`: Group name to store artifacts for this profile under
- `options`: dictionary of options to provide to the tool

For `ArtifactTool` types, automatic parsing of string options to `long`, `ulong`, and `bool` is supported.

Note: The `artifactList` option is a list of artifact IDs generated for profiles exported via the `db list` command. However, this option is not yet respected by any built-in behavior for listing or list-dumping, and such functionality must be manually implemented. This may be changed at a later time.

## Browser Cookies

Some tools may require HTTP cookies to function properly or to get as complete data as possible.

Cookies can be extracted from supported browsers via the `cookieBrowser`, `cookieBrowserDomains`, and `cookieBrowserProfile` profile options.

- `cookieBrowser`: short name of browser
  - `edge`: Microsoft Edge, Windows / macOS
  - `chrome`: Google Chrome, Windows / macOS
- `cookieBrowserDomains`: array of base domains to filter by, e.g. `["contoso.com", "fabrikam.com"]`
- `cookieBrowserProfile`: user profile name (can be profile data directory name, or user-facing profile name)

Note: The `cookieFile` option is also provided for rudimentary Netscape cookie format support. Avoid using unknown third-party extensions - find, validate, and build an open-source browser plugin for this if possible.

## Related

[sqz269's BooruScraper](https://github.com/sqz269/BooruScraper) is epic for grabbing all the pics of your waifus.
