using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net;
using System.Text;
using Art.BrowserCookies;

namespace kix.Commands.Cookie;

public class CookieCommandExtract : CommandBase
{
    protected Option<string> BrowserOption;
    protected Option<string> BrowserProfileOption;
    protected Option<List<string>> DomainsOption;

    public CookieCommandExtract(string name, string? description = null) : base(name, description)
    {
        BrowserOption = new Option<string>(new[] { "-b", "--browser" }, "Browser name") { ArgumentHelpName = "name", IsRequired = true };
        AddOption(BrowserOption);
        BrowserProfileOption = new Option<string>(new[] { "-p", "--profile" }, "Browser profile") { ArgumentHelpName = "name" };
        AddOption(BrowserProfileOption);
        DomainsOption = new Option<List<string>>(new[] { "-d", "--domain" }, "Domain(s) to filter by") { ArgumentHelpName = "domain", IsRequired = true, Arity = ArgumentArity.OneOrMore };
        AddOption(DomainsOption);
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        string browserName = context.ParseResult.GetValueForOption(BrowserOption)!;
        string? browserProfile = context.ParseResult.HasOption(BrowserProfileOption) ? context.ParseResult.GetValueForOption(BrowserProfileOption) : null;
        List<string> domains = context.ParseResult.GetValueForOption(DomainsOption)!;
        if (!CookieSource.TryGetBrowserFromName(browserName, out var source, browserProfile))
        {
            PrintErrorMessage($"Failed to find browser with name {browserName}");
            return 2;
        }
        CookieContainer cc = new();
        foreach (string domain in domains)
        {
            // TODO better api from doing multiple queries in one call, reusing cached context to avoid repeated auth
            // TODO expand implementation, to allow considering subdomains
            await source.LoadCookiesAsync(cc, domain);
        }
        TextWriter output = Console.Out;
        StringBuilder sb = new();
        foreach (object? cookie in cc.GetAllCookies())
        {
            System.Net.Cookie c = (System.Net.Cookie)cookie;
            sb.Append(c.Domain).Append('\t');
            sb.Append(c.Domain.StartsWith('.') ? "TRUE" : "FALSE").Append('\t');
            sb.Append(c.Path).Append('\t');
            sb.Append(c.Secure ? "TRUE" : "FALSE").Append('\t');
            sb.Append((long)c.Expires.Subtract(DateTime.UnixEpoch).TotalSeconds).Append('\t');
            sb.Append(c.Name).Append('\t');
            sb.Append(c.Value).Append('\n');
            await output.WriteAsync(sb.ToString());
            sb.Clear();
        }
        return 0;
    }
}
