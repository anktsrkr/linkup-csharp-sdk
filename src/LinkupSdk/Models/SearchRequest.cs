using System.Text.Json.Serialization;

namespace LinkupSdk.Models;

/// <summary>
/// Enumeration for search depth options
/// </summary>
public enum SearchDepth
{
    standard,
    deep
}

/// <summary>
/// Enumeration for output type options
/// </summary>
public enum OutputType
{
    searchResults,
    sourcedAnswer,
    structured
}

/// <summary>
/// Parameters for search requests
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// The search query
    /// </summary>
    [JsonPropertyName("q")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The depth of the search
    /// </summary>
    [JsonPropertyName("depth")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SearchDepth Depth { get; set; }

    /// <summary>
    /// The type of output to return
    /// </summary>
    [JsonPropertyName("outputType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OutputType OutputType { get; set; }

    /// <summary>
    /// Whether to include images in the results
    /// </summary>
    [JsonPropertyName("includeImages")]
    public bool? IncludeImages { get; set; }

    /// <summary>
    /// Domains to include in the search
    /// </summary>
    [JsonPropertyName("includeDomains")]
    public string[]? IncludeDomains { get; set; }

    /// <summary>
    /// Domains to exclude from the search
    /// </summary>
    [JsonPropertyName("excludeDomains")]
    public string[]? ExcludeDomains { get; set; }

    /// <summary>
    /// Start date for date range filtering
    /// </summary>
    [JsonPropertyName("fromDate")]
    public string? FromDate { get; set; }

    /// <summary>
    /// End date for date range filtering
    /// </summary>
    [JsonPropertyName("toDate")]
    public string? ToDate { get; set; }

    /// <summary>
    /// Whether to include inline citations (for sourced answers)
    /// </summary>
    [JsonPropertyName("includeInlineCitations")]
    public bool? IncludeInlineCitations { get; set; }

    /// <summary>
    /// Whether to include sources (for structured output)
    /// </summary>
    [JsonPropertyName("includeSources")]
    public bool? IncludeSources { get; set; }

    /// <summary>
    /// Schema for structured output
    /// </summary>
    [JsonPropertyName("structuredOutputSchema")]
    public string? StructuredOutputSchema { get; set; }
}
