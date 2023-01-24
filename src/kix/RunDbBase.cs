using CommandLine;

namespace Kix;

internal class RunDbBase : IRunOutput
{
    [Option('v', "verbose", HelpText = "Enable verbose output.")]
    public bool Verbose { get; set; }

    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = Common.DefaultDbFile)]
    public string Database { get; set; } = null!;

    [Option('t', "tool", HelpText = "Tool to filter by.", MetaValue = "pattern")]
    public string? Tool { get; set; }

    [Option('g', "group", HelpText = "Group to filter by.", MetaValue = "pattern")]
    public string? Group { get; set; }

    [Option('i', "id", HelpText = "Id to filter by.", MetaValue = "pattern")]
    public string? Id { get; set; }

    [Option("tool-like", HelpText = "Tool pattern to filter by.", MetaValue = "pattern")]
    public string? ToolLike { get; set; }

    [Option("group-like", HelpText = "Group pattern to filter by.", MetaValue = "pattern")]
    public string? GroupLike { get; set; }

    [Option("id-like", HelpText = "Id pattern to filter by.", MetaValue = "pattern")]
    public string? IdLike { get; set; }

    [Option("name-like", HelpText = "Name pattern to filter by.", MetaValue = "pattern")]
    public string? NameLike { get; set; }

    [Option('l', "list-resource", HelpText = "List resource items.")]
    public bool ListResource { get; set; }

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }
}
