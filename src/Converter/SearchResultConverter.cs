using LinkupSdk.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinkupSdk.Converter;

/// <summary>
/// Custom JSON converter for SearchResult polymorphic deserialization
/// </summary>
public class SearchResultConverter : JsonConverter<SearchResult>
{
    public override SearchResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("type", out var typeProperty))
        {
            var type = typeProperty.GetString();
            return type switch
            {
                "text" => JsonSerializer.Deserialize<TextSearchResult>(root.GetRawText(), options),
                "image" => JsonSerializer.Deserialize<ImageSearchResult>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown search result type: {type}")
            };
        }

        throw new JsonException("Missing 'type' property in search result");
    }

    public override void Write(Utf8JsonWriter writer, SearchResult value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
