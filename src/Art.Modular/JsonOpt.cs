using System.Text.Json;

namespace Art.Modular;

public static class JsonOpt
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip };
}
