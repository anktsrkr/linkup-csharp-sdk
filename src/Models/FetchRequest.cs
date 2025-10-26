using System.Text.Json.Serialization;

namespace LinkupSdk.Models;

/// <summary>
/// Request model for fetch API calls
/// </summary>
public class FetchRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("renderJs")]
    public bool? RenderJs { get; set; }

    [JsonPropertyName("includeRawHtml")]
    public bool? IncludeRawHtml { get; set; }

    [JsonPropertyName("extractImages")]
    public bool? ExtractImages { get; set; }
}
