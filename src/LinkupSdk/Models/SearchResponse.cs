using LinkupSdk.Converter;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinkupSdk.Models;

/// <summary>
/// Abstract base class for search API responses
/// </summary>
[JsonConverter(typeof(SearchResponseConverter))]
public abstract class SearchResponse
{
}

/// <summary>
/// Response model for search results
/// </summary>
public class SearchResultsResponse : SearchResponse
{
    [JsonPropertyName("results")]
    public SearchResult[]? Results { get; set; }
}

/// <summary>
/// Response model for sourced answer
/// </summary>
public class SourcedAnswerResponse : SearchResponse
{
    [JsonPropertyName("answer")]
    public string? Answer { get; set; }

    [JsonPropertyName("sources")]
    public Source[]? Sources { get; set; }
}

/// <summary>
/// Response model for structured output
/// </summary>
public class StructuredResponseWithSources<T> : SearchResponse
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("sources")]
    public SearchResult[]? Sources { get; set; }
}

/// <summary>
/// Response model for structured output with sources (non-generic for deserialization)
/// </summary>
public class StructuredResponseWithSources : SearchResponse
{
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    [JsonPropertyName("sources")]
    public SearchResult[]? Sources { get; set; }
}


/// <summary>
/// Response model for structured output
/// </summary>
[JsonConverter(typeof(SearchResponseConverter))]
public class StructuredResponse<T> : SearchResponse
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// Response model for structured output (non-generic for deserialization)
/// </summary>
public class StructuredResponse : SearchResponse
{
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

/// <summary>
/// Abstract base class for search results
/// </summary>
[JsonConverter(typeof(SearchResultConverter))]
public abstract class SearchResult
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Text search result
/// </summary>
public sealed class TextSearchResult : SearchResult
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Image search result
/// </summary>
public class ImageSearchResult : SearchResult
{
}



/// <summary>
/// Source information for search results
/// </summary>
public class Source
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;
}

