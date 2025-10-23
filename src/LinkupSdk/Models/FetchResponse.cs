using System.Text.Json.Serialization;

namespace LinkupSdk.Models;

/// <summary>
/// Response model for fetch API calls
/// </summary>
public class FetchResponse
{
    [JsonPropertyName("markdown")]
    public string Markdown { get; set; } = string.Empty;

    [JsonPropertyName("rawHtml")]
    public string? RawHtml { get; set; }

    [JsonPropertyName("images")]
    public ExtractedImage[]? Images { get; set; }
}

/// <summary>
/// Image information extracted from fetched content
/// </summary>
public class ExtractedImage
{
    [JsonPropertyName("alt")]
    public string Alt { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
