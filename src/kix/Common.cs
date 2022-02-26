using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Art;
using Art.Management;
using Art.Resources;
using EA;

namespace Kix;

internal static class Common
{
    internal static async Task DisplayAsync(ArtifactInfo i, bool listResource, ArtifactRegistrationManager arm, bool detailed)
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

    internal static ArtifactToolProfile GetWithConsoleOptions(this ArtifactToolProfile artifactToolProfile, string? cookieFile, IEnumerable<string> properties)
    {
        Dictionary<string, JsonElement> opts = artifactToolProfile.Options != null
            ? new Dictionary<string, JsonElement>(artifactToolProfile.Options)
            : new Dictionary<string, JsonElement>();
        if (cookieFile != null) opts.AddPropWithWarning("cookieFile", cookieFile.J());
        opts.AddProps(properties);
        return artifactToolProfile with { Options = opts };
    }

    internal static Assembly LoadAssemblyForToolString(string toolString)
    {
        (string assembly, _) = ArtifactToolProfile.GetId(toolString);
        KixManifest manifest;
        try
        {
            manifest = KixManifest.GetManifests().Single(v => string.Equals(v.Content.Assembly, assembly, StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            throw new InvalidOperationException($"No applicable manifest for the assembly {assembly} could be found");
        }
        return LoadAssemblyForManifest(manifest);
    }

    internal static Assembly LoadAssemblyForManifest(KixManifest manifest)
    {
        string assemblyPath = manifest.Content.Path != null && !Path.IsPathFullyQualified(manifest.Content.Path)
            ? Path.Combine(manifest.BasePath, manifest.Content.Path, Path.ChangeExtension(manifest.Content.Assembly, ".dll"))
            : Path.Combine(manifest.BasePath, Path.ChangeExtension(manifest.Content.Assembly, ".dll"));
        return Assembly.LoadFrom(assemblyPath);
    }
}
