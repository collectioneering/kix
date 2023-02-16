﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Art;

namespace kix;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<ArtifactToolProfile>))]
[JsonSerializable(typeof(JsonElement))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
