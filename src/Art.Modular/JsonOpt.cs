using System.Text.Json;

namespace Art.Modular;

public static class JsonOpt
{
    public static JsonSerializerOptions GetOptions() => new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip };
}
