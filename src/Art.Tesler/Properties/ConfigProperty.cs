using System.Text.Json;

namespace Art.Tesler.Properties;

public record struct ConfigProperty(ConfigScope ConfigScope, string Key, JsonElement Value);