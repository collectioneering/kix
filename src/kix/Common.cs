using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Art;
using Art.BrowserCookies;
using Art.Common;
using Art.Common.Logging;
using EA;

namespace kix;

internal static class Common
{
    internal const string ChecksumAlgorithms = "None|SHA1|SHA256|SHA384|SHA512|MD5";
    internal const string ResourceUpdateModes = $"{nameof(ResourceUpdateMode.ArtifactSoft)}|{nameof(ResourceUpdateMode.ArtifactHard)}|{nameof(ResourceUpdateMode.Soft)}|{nameof(ResourceUpdateMode.Hard)}";
    internal const string ArtifactSkipModes = $"{nameof(ArtifactSkipMode.None)}|{nameof(ArtifactSkipMode.FastExit)}|{nameof(ArtifactSkipMode.Known)}";
    internal const string DefaultDbFile = "kix_data.db";

    internal static IToolLogHandler GetDefaultToolLogHandler() => OperatingSystem.IsMacOS() ? ConsoleLogHandler.Fancy : ConsoleLogHandler.Default;

    internal static int AccumulateErrorCode(int errorCode, int existingErrorCode)
    {
        return errorCode != 0 ? errorCode : existingErrorCode;
    }

    internal static async Task DisplayAsync(ArtifactInfo i, bool listResource, IArtifactRegistrationManager arm, bool detailed)
    {
        Display(i, detailed);
        if (listResource)
            foreach (ArtifactResourceInfo r in await arm.ListResourcesAsync(i.Key))
                Display(r, detailed);
    }

    internal static async Task DisplayAsync(ArtifactInfo i, IEnumerable<ArtifactResourceInfo> resources, bool detailed)
    {
        Display(i, detailed);
        foreach (ArtifactResourceInfo r in resources)
            if (r.UsesMetadata)
                try
                {
                    ArtifactResourceInfo r2 = await r.WithMetadataAsync();
                    Display(r2, detailed);
                }
                catch
                {
                    Display(r, detailed);
                }
            else
                Display(r, detailed);
    }

    internal static void PrintFormat(string entry, bool detailed, Func<string> details)
    {
        Console.WriteLine(entry);
        if (detailed)
        {
            Console.WriteLine(new string('-', EastAsianWidth.GetWidth(entry)));
            Console.WriteLine(details());
            Console.WriteLine();
        }
    }

    internal static void Display(ArtifactInfo i, bool detailed)
        => PrintFormat(i.Key.Tool + "/" + i.Key.Group + ": " + i.GetInfoTitleString(), detailed, i.GetInfoString);

    internal static void Display(ArtifactResourceInfo r, bool detailed)
        => PrintFormat("-- " + r.GetInfoPathString(), detailed, r.GetInfoString);

    private static readonly Regex s_propRe = new(@"(.+?):(.+)");

    internal static void AddProps(this Dictionary<string, JsonElement> dictionary, IEnumerable<string> props)
    {
        foreach (string prop in props)
        {
            if (s_propRe.Match(prop) is not { Success: true } match) throw new ArgumentException($@"Invalid property entry ""{prop}""");
            string k = match.Groups[1].Value;
            string val = match.Groups[2].Value;
            JsonElement v;
            if (val.StartsWith('{') || val.StartsWith('[')) v = JsonSerializer.Deserialize<JsonElement>(val);
            else if (long.TryParse(val, out long valLong)) v = JsonSerializer.SerializeToElement(valLong);
            else if (ulong.TryParse(val, out ulong valULong)) v = JsonSerializer.SerializeToElement(valULong);
            else if (double.TryParse(val, out double valDouble)) v = JsonSerializer.SerializeToElement(valDouble);
            else v = JsonSerializer.SerializeToElement(val);
            dictionary.AddPropWithWarning(k, v);
        }
    }

    private static void AddPropWithWarning(this Dictionary<string, JsonElement> dictionary, string k, JsonElement v)
    {
        if (dictionary.ContainsKey(k)) Console.WriteLine($@"Warning: property {k} already exists with value ""{dictionary[k].ToString()}"", overwriting");
        dictionary[k] = v;
    }

    // https://stackoverflow.com/a/4146349
    internal static Regex GetFilterRegex(string pattern, bool caseSensitive, bool full) => new(
        (full ? "^" : "") + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + (full ? "$" : ""),
        (caseSensitive ? 0 : RegexOptions.IgnoreCase) | RegexOptions.Singleline);

    internal static IEnumerable<ArtifactInfo> WithFilters(this IEnumerable<ArtifactInfo> enumerable, string? tool, string? toolLike, string? group, string? groupLike, string? id, string? idLike, string? nameLike)
    {
        if (id != null) enumerable = enumerable.Where(v => v.Key.Id == id);
        if (toolLike != null && tool == null)
        {
            Regex r = GetFilterRegex(toolLike, false, false);
            enumerable = enumerable.Where(v => r.IsMatch(v.Key.Tool));
        }
        if (groupLike != null && group == null)
        {
            Regex r = GetFilterRegex(groupLike, false, false);
            enumerable = enumerable.Where(v => r.IsMatch(v.Key.Group));
        }
        if (idLike != null && id == null)
        {
            Regex r = GetFilterRegex(idLike, false, false);
            enumerable = enumerable.Where(v => r.IsMatch(v.Key.Id));
        }
        if (nameLike != null)
        {
            Regex r = GetFilterRegex(nameLike, false, false);
            enumerable = enumerable.Where(v => v.Name != null && r.IsMatch(v.Name));
        }
        return enumerable;
    }

    internal static string GetInvalidHashMessage(string hash)
    {
        return new StringBuilder($"Failed to find hash algorithm {hash}")
            .Append(Console.Error.NewLine)
            .Append("Known algorithms:")
            .Append(Console.Error.NewLine)
            .AppendJoin(Console.Error.NewLine, ChecksumSource.DefaultSources.Values.Select(v => v.Id))
            .ToString();
    }

    internal static string GetInvalidCookieSourceBrowserMessage(string browserName)
    {
        return new StringBuilder($"Failed to find browser with name {browserName}")
            .Append(Console.Error.NewLine)
            .Append("Supported browsers:")
            .Append(Console.Error.NewLine)
            .AppendJoin(Console.Error.NewLine, CookieSource.GetSupportedBrowserNames())
            .ToString();
    }

    internal static ArtifactToolProfile GetWithConsoleOptions(this ArtifactToolProfile artifactToolProfile, string? cookieFile, IEnumerable<string> properties)
    {
        Dictionary<string, JsonElement> opts = artifactToolProfile.Options != null
            ? new Dictionary<string, JsonElement>(artifactToolProfile.Options)
            : new Dictionary<string, JsonElement>();
        if (cookieFile != null) opts.AddPropWithWarning("cookieFile", JsonSerializer.SerializeToElement(cookieFile));
        opts.AddProps(properties);
        return artifactToolProfile with { Options = opts };
    }
}
