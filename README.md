# kix

The `kix` program provides a simple way to run tools designed with the [Art](https://github.com/collectioneering/Art) library.

Artifact registration is managed by Sqlite database files, and artifact data (resources) are written to a selected folder on disk.

No tools are directly provided with this program.

## Features

- `arc`: Performs unified archival using tools by artifact location or profile file
- `find`/`list`/`dump`: Executes tools by tool+group or profile file
  - Contrary to `arc` and `dump`, `find` and `list` only display information on the artifacts found by the tool.
  - `arc` and `dump` are similar in that they can both output artifact data.
    - `arc` has the benefit of automatic tool selection for a passed artifact location
    and options useful for operation against an existing database.
    - `dump` always dumps the artifacts in question. It also supports running a specific tool without passing in a profile file.
    - `arc` is preferred in most cases, especially for repeated archival against a profile or one-off runs against an artifact location. `dump` is preferred for one-off runs of dump tools that wouldn't require writing a profile file.
- `rehash`: Recomputes resource hashes with the specified hash algorithm
- `validate`: Validates resource data based on stored hash
  - Optionally repairs resources that failed validation
- `tools`: Enumerates available plugins
- `config`: Provides get/set/unset of options for a profile, a tool, or the runner program
- `stream`: Streams the primary resource stream of the artifact identified by the passed profile file or artifact location
- `cookie`: Provides cookie extraction for a few supported OS+browser+cookie combinations
- `db`: Provides limited Sqlite database management (list, delete, merge, cleanup)

## Artifact location

For the `arc` and `stream` commands, if an input argument does not appear to be a profile file, all tool factory classes implementing `IArtifactToolSelector<string>` in the available plugins are queried. One can declare that the passed value points to an artifact that should be processed by a certain tool (typically the same tool type as the one the factory class creates).

For example, a tool for a website `example.com` could know that articles are available under `https://example.com/posts/<ID>`, so the value `https://example.com/posts/123` should be processed by the find tool with the artifact ID `123`. The `IArtifactToolRegexSelector<TSelf>` interface provides syntax sugar for regex-based selectors.

Example:

```csharp
internal partial class ExampleTool : ArtifactTool, IArtifactToolSelfFactory<ExampleTool>, IArtifactToolRegexSelector<ExampleTool>
{
    // Regex to test inputs against
    [GeneratedRegex(@"^https://example\.com/id/(?<ID_GROUP>\d+)$")]
    public static partial Regex GetArtifactToolSelectorRegex();

    // Regex group name that represents the ID portion
    public static string GetArtifactToolSelectorRegexIdGroupName() => "ID_GROUP";
}
```

## Plugins

### File Structure

Each plugin should be placed under `Plugins/<some-name>.kix/`, e.g. `Plugins/MyToolAssembly.kix/`.

### Plugin Loading

Plugins are loaded into separate [`AssemblyLoadContext`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext)s and are primarily manipulated under the `Art` interface assembly. Tool assemblies should be compiled with the same Art interface library that `kix` is compiled with to ensure interface compatibility. Resolution of the `Art` assembly always falls through to the program's default [`AssemblyLoadContext`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext) for interface purposes, while other dependencies are resolved by an [`AssemblyDependencyResolver`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblydependencyresolver), which uses the corresponding `.deps.json` file to correctly resolve platform-specific managed and native dependencies.

### Plugin Creation

Plugins should contain one or more tools that implement one of `IArtifactTool`'s sub-interfaces, such as `IArtifactListTool`, `IArtifactFindTool`, or `IArtifactDumpTool`. The base classes `ArtifactTool` (from package [Art.Common](https://www.nuget.org/packages/Art.Common)), `HttpArtifactTool` (from package [Art.Http](https://www.nuget.org/packages/Art.Http)), and `HtmlArtifactTool` (from package [Art.Html](https://www.nuget.org/packages/Art.Html)) are useful starting points.

It is recommended to create an `IArtifactFindTool` tool as a base type when applicable, and mark it with `CoreAttribute`. This lets different derived tools - such as an `IArtifactListTool` that performs queries and another `IArtifactListTool` that lists items based on author - create artifacts keyed by the base type (at least by default for `ArtifactTool` subtypes). This facilitates unified management, updating, and repairing of artifacts.

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

Note: The `artifactList` option is a list of artifact IDs generated for profiles exported via the `db list` command.

## Browser Cookies

Some tools may require HTTP cookies to function properly or to get as complete data as possible.

### Browser Cookies with HttpArtifactTool

For `HttpArtifactTool`-derived tools, cookies are loaded based on the presence of specific options.

Cookies can be extracted from supported browsers via the `cookieBrowser`, `cookieBrowserDomains`, and `cookieBrowserProfile` profile options.

- `cookieBrowser`: short name of browser
  - `edge`: Microsoft Edge, Windows / macOS
  - `chrome`: Google Chrome, Windows / macOS
- `cookieBrowserDomains`: array of base domains to filter by, e.g. `["contoso.com", "fabrikam.com"]`
- `cookieBrowserProfile`: user profile name (can be profile data directory name, or user-facing profile name)

[Details on specific supported cookie types are available here.](https://github.com/collectioneering/Art/blob/main/src/Art.BrowserCookies/README.md)

The `cookieFile` option is also provided for rudimentary Netscape cookie format support. Avoid using unknown third-party extensions - find, validate, and build an open-source browser plugin for this if possible.

## Related

[sqz269's BooruScraper](https://github.com/sqz269/BooruScraper) is epic for grabbing all the pics of your waifus.
