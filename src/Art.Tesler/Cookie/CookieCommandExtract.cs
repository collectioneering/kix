using System.CommandLine;
using System.Net;
using System.Text;
using Art.BrowserCookies;

namespace Art.Tesler.Cookie;

public class CookieCommandExtract : CommandBase
{
    protected Option<string> BrowserOption;
    protected Option<string> BrowserProfileOption;
    protected Option<List<string>> DomainsOption;
    protected Option<string> OutputOption;
    protected Option<bool> NoSubdomainsOption;
    private readonly IToolLogHandlerProvider _toolLogHandlerProvider;

    public CookieCommandExtract(IToolLogHandlerProvider toolLogHandlerProvider, string name, string? description = null) : base(toolLogHandlerProvider, name, description)
    {
        BrowserOption = new Option<string>("-b", "--browser") { HelpName = "name", Required = true, Description = "Browser name" };
        Add(BrowserOption);
        BrowserProfileOption = new Option<string>("-p", "--profile") { HelpName = "name", Description = "Browser profile" };
        Add(BrowserProfileOption);
        DomainsOption = new Option<List<string>>("-d", "--domain") { HelpName = "domain", Required = true, Arity = ArgumentArity.OneOrMore, Description = "Domain(s) to filter by" };
        Add(DomainsOption);
        OutputOption = new Option<string>("-o", "--output") { HelpName = "file", Description = "Output filename" };
        Add(OutputOption);
        NoSubdomainsOption = new Option<bool>("--no-subdomains") { Description = "Do not include subdomains" };
        Add(NoSubdomainsOption);
        _toolLogHandlerProvider = toolLogHandlerProvider;
    }

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string browserName = parseResult.GetRequiredValue(BrowserOption);
        string? browserProfile = parseResult.GetValue(BrowserProfileOption);
        List<string> domains = parseResult.GetRequiredValue(DomainsOption);
        bool includeSubdomains = !parseResult.GetValue(NoSubdomainsOption);
        if (!CookieSource.TryGetBrowserFromName(browserName, out var source, browserProfile))
        {
            PrintErrorMessage(Common.GetInvalidCookieSourceBrowserMessage(browserName), ToolOutput);
            return 2;
        }
        source = source.Resolve();
        if (parseResult.GetValue(OutputOption) is { } outputPath)
        {
            var output = File.CreateText(outputPath);
            await using var output1 = output.ConfigureAwait(false);
            await ExportAsync(source, domains, includeSubdomains, output, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await ExportAsync(source, domains, includeSubdomains, ToolOutput.Out, cancellationToken).ConfigureAwait(false);
        }
        return 0;
    }

    private async Task ExportAsync(CookieSource source, IEnumerable<string> domains, bool includeSubdomains, TextWriter output, CancellationToken cancellationToken)
    {
        CookieContainer cc = new();
        var logHandler = _toolLogHandlerProvider.GetDefaultToolLogHandler(LogPreferences.Default);
        await source.LoadCookiesAsync(
                cc,
                domains.Select(v => new CookieFilter(v, includeSubdomains)).ToList(),
                logHandler.LogInformation,
                cancellationToken)
            .ConfigureAwait(false);
        await output.WriteAsync("# Netscape HTTP Cookie File\n").ConfigureAwait(false);
        StringBuilder sb = new();
        foreach (object? cookie in cc.GetAllCookies())
        {
            System.Net.Cookie c = (System.Net.Cookie)cookie;
            sb.Append(c.Domain).Append('\t');
            sb.Append(c.Domain.StartsWith('.') ? "TRUE" : "FALSE").Append('\t');
            sb.Append(c.Path).Append('\t');
            sb.Append(c.Secure ? "TRUE" : "FALSE").Append('\t');
            sb.Append(c.Expires == DateTime.MinValue ? 0 : (long)c.Expires.Subtract(DateTime.UnixEpoch).TotalSeconds).Append('\t');
            sb.Append(c.Name).Append('\t');
            sb.Append(c.Value).Append('\n');
            await output.WriteAsync(sb.ToString()).ConfigureAwait(false);
            sb.Clear();
        }
    }
}
