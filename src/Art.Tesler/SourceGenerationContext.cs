using System.Text.Json;
using System.Text.Json.Serialization;

namespace Art.Tesler;

internal partial class SourceGenerationContext
{
    internal static SourceGenerationContextImpl s_context => SourceGenerationContextImpl.s_context;

    //[JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(List<ArtifactToolProfile>))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonElement[]))]
    [JsonSerializable(typeof(IReadOnlyDictionary<string, JsonElement>))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSerializable(typeof(Refactorings))]
    [JsonSerializable(typeof(object))]
    internal partial class SourceGenerationContextImpl : JsonSerializerContext
    {
        static SourceGenerationContextImpl()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip };
            options.Converters.Add(new JsonStringEnumConverter());
            s_context = new SourceGenerationContextImpl(options);
        }

        internal static readonly SourceGenerationContextImpl s_context;
    }
}