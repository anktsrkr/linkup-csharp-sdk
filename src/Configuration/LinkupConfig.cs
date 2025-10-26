namespace LinkupSdk.Configuration;

/// <summary>
/// Configuration class for Linkup API client
/// </summary>
public class LinkupConfig
{
    /// <summary>
    /// The API key for authentication
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Optional base URL for the API (defaults to https://api.linkup.so/v1/)
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Endpoint for search requests (defaults to "/search")
    /// </summary>
    public string SearchEndpoint { get; set; } = "search";

    /// <summary>
    /// Endpoint for fetch requests (defaults to "/fetch")
    /// </summary>
    public string FetchEndpoint { get; set; } = "fetch";

    /// <summary>
    /// Endpoint for balance requests (defaults to "/credits/balance")
    /// </summary>
    public string BalanceEndpoint { get; set; } = "credits/balance";
}