using LinkupSdk.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinkupSdk.Converter;


/// <summary>
/// Custom JSON converter for SearchResponse polymorphic deserialization
/// </summary>
public class SearchResponseConverter : JsonConverter<SearchResponse>
{
    public override SearchResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Determine response type based on properties
        if (root.TryGetProperty("results", out _))
        {
            return JsonSerializer.Deserialize<SearchResultsResponse>(root.GetRawText(), options);
        }
        else if (root.TryGetProperty("answer", out _))
        {
            return JsonSerializer.Deserialize<SourcedAnswerResponse>(root.GetRawText(), options);
        }

        throw new JsonException("Unable to determine response type from JSON properties");
    }

    public override void Write(Utf8JsonWriter writer, SearchResponse value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
