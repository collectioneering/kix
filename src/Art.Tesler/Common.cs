using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Art.BrowserCookies;
using Art.Common;
using Art.Tesler.Properties;
using EA;

namespace Art.Tesler;

internal static class Common
{
    private static readonly string[] ChecksumAlgorithmArray = ChecksumSource.DefaultSources.Values.Select(v => v.Id).ToArray();
    internal static readonly string ChecksumAlgorithms = new StringBuilder().AppendJoin('|', ChecksumAlgorithmArray.Prepend("None")).ToString();
    internal const string DefaultChecksumAlgorithm = "SHA256";
    internal const string DefaultDbFile = "kix_data.db";

    internal static async Task DisplayAsync(
        ArtifactInfo i,
        bool listResource,
        IArtifactRegistrationManager arm,
        bool detailed,
        IOutputControl console,
        ArtifactKey? refactoredArtifactKey = null
    )
    {
        Display(refactoredArtifactKey != null ? i with { Key = refactoredArtifactKey } : i, detailed, console);
        if (listResource)
        {
            foreach (ArtifactResourceInfo r in await arm.ListResourcesAsync(i.Key).ConfigureAwait(false))
                Display(refactoredArtifactKey != null ? r with { Key = r.Key with { Artifact = refactoredArtifactKey } } : r, detailed, console);
        }
    }

    internal static Task DisplayAsync(
        IArtifactData d,
        bool listResource,
        bool detailed,
        IOutputControl console,
        LogPreferences logPreferences)
    {
        if (console is ObjectToolLogHandlerProvider provider)
        {
            var handler = provider.GetDefaultToolLogHandler(logPreferences);
            handler.Log(new ArtifactDataObjectLog(null, null, LogLevel.Information, d));
            return Task.CompletedTask;
        }
        if (listResource)
        {
            return DisplayAsync(d.Info, d.Values, detailed, console);
        }
        Display(d.Info, detailed, console);
        return Task.CompletedTask;
    }

    private static async Task DisplayAsync(ArtifactInfo i, IEnumerable<ArtifactResourceInfo> resources, bool detailed, IOutputControl console)
    {
        Display(i, detailed, console);
        foreach (ArtifactResourceInfo r in resources)
        {
            if (r.UsesMetadata)
            {
                try
                {
                    ArtifactResourceInfo r2 = await r.WithMetadataAsync().ConfigureAwait(false);
                    Display(r2, detailed, console);
                }
                catch
                {
                    Display(r, detailed, console);
                }
            }
            else
            {
                Display(r, detailed, console);
            }
        }
    }

    internal static void PrintFormat(string entry, bool detailed, Func<string> details, IOutputControl console)
    {
        console.Out.WriteLine(entry);
        if (detailed)
        {
            console.Out.WriteLine(new string('-', EastAsianWidth.GetWidth(entry)));
            console.Out.WriteLine(details());
            console.Out.WriteLine("");
        }
    }

    private static void Display(ArtifactInfo i, bool detailed, IOutputControl console)
    {
        PrintFormat(i.Key.Tool + "/" + i.Key.Group + ": " + i.GetInfoTitleString(), detailed, i.GetInfoString, console);
    }

    internal static void Display(ArtifactResourceInfo r, bool detailed, IOutputControl console)
    {
        PrintFormat("-- " + r.GetInfoPathString(), detailed, r.GetInfoString, console);
    }

    internal static JsonElement ParsePropToJsonElement(string prop)
    {
        if (prop.StartsWith('{') || prop.StartsWith('[') || prop.StartsWith('"'))
        {
            return JsonSerializer.Deserialize(prop, SourceGenerationContext.s_context.JsonElement);
        }
        else if (long.TryParse(prop, out long valLong))
        {
            return JsonSerializer.SerializeToElement(valLong, SourceGenerationContext.s_context.Int64);
        }
        else if (ulong.TryParse(prop, out ulong valULong))
        {
            return JsonSerializer.SerializeToElement(valULong, SourceGenerationContext.s_context.UInt64);
        }
        else if (double.TryParse(prop, out double valDouble))
        {
            return JsonSerializer.SerializeToElement(valDouble, SourceGenerationContext.s_context.Double);
        }
        else if (string.Equals(prop, "null", StringComparison.InvariantCulture))
        {
            return JsonSerializer.SerializeToElement(null, (JsonTypeInfo)SourceGenerationContext.s_context.Object);
        }
        else if (string.Equals(prop, "true", StringComparison.InvariantCulture))
        {
            return JsonSerializer.SerializeToElement(true, SourceGenerationContext.s_context.Boolean);
        }
        else if (string.Equals(prop, "false", StringComparison.InvariantCulture))
        {
            return JsonSerializer.SerializeToElement(false, SourceGenerationContext.s_context.Boolean);
        }
        else
        {
            return JsonSerializer.SerializeToElement(prop, SourceGenerationContext.s_context.String);
        }
    }

    private static readonly Regex s_propRe = new(@"(.+?):(.+)");

    internal static void AddProps(this Dictionary<string, JsonElement> dictionary, IEnumerable<string> props, IOutputControl console)
    {
        foreach (string prop in props)
        {
            if (s_propRe.Match(prop) is not { Success: true } match)
            {
                throw new ArgumentException($@"Invalid property entry ""{prop}""");
            }
            string k = match.Groups[1].Value;
            string val = match.Groups[2].Value;
            dictionary.AddPropWithWarning(k, ParsePropToJsonElement(val), console);
        }
    }

    internal static void AddPropElements(this Dictionary<string, JsonElement> dictionary, IEnumerable<string> props, IOutputControl console)
    {
        foreach (string prop in props)
        {
            if (s_propRe.Match(prop) is not { Success: true } match)
            {
                throw new ArgumentException($@"Invalid property entry ""{prop}""");
            }
            string k = match.Groups[1].Value;
            string val = match.Groups[2].Value;
            dictionary.AddPropElement(k, ParsePropToJsonElement(val));
        }
    }

    private static void AddPropWithWarning(this Dictionary<string, JsonElement> dictionary, string k, JsonElement v, IOutputControl console)
    {
        if (dictionary.ContainsKey(k)) console.Warn.WriteLine($@"Warning: property {k} already exists with value ""{dictionary[k].ToString()}"", overwriting");
        dictionary[k] = v;
    }

    private static void AddPropElement(this Dictionary<string, JsonElement> dictionary, string k, JsonElement v)
    {
        if (!dictionary.ContainsKey(k))
        {
            dictionary[k] = JsonSerializer.SerializeToElement([v], SourceGenerationContext.s_context.JsonElementArray);
            return;
        }
        JsonElement existing = dictionary[k];
        if (existing.ValueKind != JsonValueKind.Array)
        {
            throw new ArtUserException($"Property {k} is a {existing.ValueKind}, cannot add an element value to a non-{JsonValueKind.Array} value");
        }
        var value = existing.Deserialize<JsonElement[]>(SourceGenerationContext.s_context.JsonElementArray) ?? [];
        dictionary[k] = JsonSerializer.SerializeToElement([..value, v], SourceGenerationContext.s_context.JsonElementArray);
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
            .Append(Environment.NewLine)
            .Append("Known algorithms:")
            .Append(Environment.NewLine)
            .AppendJoin(Environment.NewLine, ChecksumAlgorithmArray)
            .ToString();
    }

    internal static string GetInvalidCookieSourceBrowserMessage(string browserName)
    {
        return new StringBuilder($"Failed to find browser with name {browserName}")
            .Append(Environment.NewLine)
            .Append("Supported browsers:")
            .Append(Environment.NewLine)
            .AppendJoin(Environment.NewLine, CookieSource.GetSupportedBrowserNames())
            .ToString();
    }

    internal static ArtifactToolProfile GetWithConsoleOptions(
        this ArtifactToolProfile artifactToolProfile,
        IArtifactToolRegistryStore registryStore,
        IToolPropertyProvider? toolPropertyProvider,
        IEnumerable<string> properties,
        IEnumerable<string> propertyElements,
        string? cookieFile,
        string? userAgent,
        IOutputControl console)
    {
        Dictionary<string, JsonElement> opts = new();
        if (toolPropertyProvider != null)
        {
            TeslerPropertyUtility.ApplyPropertiesDeep(registryStore, toolPropertyProvider, console, opts, artifactToolProfile.GetID());
        }
        if (artifactToolProfile.Options != null)
        {
            foreach (var pair in artifactToolProfile.Options)
            {
                opts[pair.Key] = pair.Value;
            }
        }
        if (cookieFile != null) opts.AddPropWithWarning("cookieFile", JsonSerializer.SerializeToElement(cookieFile, SourceGenerationContext.s_context.String), console);
        if (userAgent != null) opts.AddPropWithWarning("userAgent", JsonSerializer.SerializeToElement(userAgent, SourceGenerationContext.s_context.String), console);
        opts.AddProps(properties, console);
        opts.AddPropElements(propertyElements, console);
        return artifactToolProfile with { Options = opts };
    }
}
