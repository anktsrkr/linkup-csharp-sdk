using LinkupSdk.Configuration;
using LinkupSdk.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Schema;

namespace LinkupSdk.Client;

/// <summary>
/// Main client class for interacting with the Linkup API
/// </summary>
/// <remarks>
/// Initializes a new instance of the LinkupClient class
/// </remarks>
/// <param name="httpClient">The configured HttpClient instance</param>
/// <param name="config">The Linkup configuration</param>
public class LinkupClient(HttpClient httpClient, IOptionsMonitor<LinkupConfig> config)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly LinkupConfig _config = config.CurrentValue;

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSchemaExporterOptions _jsonSchemaExporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
    };


    /// <summary>
    /// Performs a search request to the Linkup API
    /// </summary>
    /// <param name="request">The search parameters</param>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>Search response with results based on the specified output type</returns>
    /// <exception cref="LinkupException">Thrown when the API returns an error response with structured error information</exception>
    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
    

        var response = await _httpClient.PostAsJsonAsync(_config.SearchEndpoint, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<LinkupErrorResponse>(cancellationToken);
            if (errorResponse != null)
            {
                throw new LinkupException(errorResponse);
            }
            else
            {
                throw new LinkupException((int)response.StatusCode, $"API request failed with status code {response.StatusCode}");
            }
        }

        if (request.OutputType == OutputType.structured)
        {
            if (request.IncludeSources == true)
            {
                var structuredResponseWithSources = await response.Content.ReadFromJsonAsync<StructuredResponseWithSources<dynamic>>(cancellationToken) ?? throw new InvalidOperationException("Failed to deserialize response");
                return structuredResponseWithSources;
            }
            else
            {
                var structuredResponse = await response.Content.ReadFromJsonAsync<dynamic>(cancellationToken) ?? throw new InvalidOperationException("Failed to deserialize response");
                return new StructuredResponse<dynamic> { Data = structuredResponse };
            }
        }
        else {
            return await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken) ?? throw new InvalidOperationException("Failed to deserialize response");
        }
    }

    /// <summary>
    /// Performs a structured search request to the Linkup API
    /// </summary>
    /// <typeparam name="T">The type to deserialize the structured data to</typeparam>
    /// <param name="request">The search parameters (OutputType must be structured)</param>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>Structured response with typed data, optionally with sources</returns>
    /// <exception cref="LinkupException">Thrown when the API returns an error response with structured error information</exception>
    /// <exception cref="InvalidOperationException">Thrown when the response is not a structured response</exception>
    public async Task<SearchResponse> SearchAsync<T>(SearchRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OutputType != OutputType.structured)
        {
            throw new ArgumentException("OutputType must be set to OutputType.structured when using the generic SearchAsync<T> method.", nameof(request));
        }
        var schema = _jsonSerializerOptions.GetJsonSchemaAsNode(typeof(T), _jsonSchemaExporterOptions);
        request.StructuredOutputSchema = schema.ToString();


        var response = await _httpClient.PostAsJsonAsync(_config.SearchEndpoint, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<LinkupErrorResponse>(cancellationToken);
            if (errorResponse != null)
            {
                throw new LinkupException(errorResponse);
            }
            else
            {
                throw new LinkupException((int)response.StatusCode, $"API request failed with status code {response.StatusCode}");
            }
        }

        // Deserialize directly to the appropriate response type based on IncludeSources
        if (request.IncludeSources == true)
        {
            var structuredResponseWithSources = await response.Content.ReadFromJsonAsync<StructuredResponseWithSources<T>>(cancellationToken) ?? throw new InvalidOperationException("Failed to deserialize response");
            return structuredResponseWithSources;
        }
        else
        {
            var structuredResponse = await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new InvalidOperationException("Failed to deserialize response");
            return new StructuredResponse<T> { Data = structuredResponse };
        }
    }

    /// <summary>
    /// Fetches content from a URL using the Linkup API
    /// </summary>
    /// <param name="parameters">The fetch parameters</param>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>Fetch response with content in the requested format</returns>
    /// <exception cref="LinkupException">Thrown when the API returns an error response with structured error information</exception>
    public async Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(_config.FetchEndpoint, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<LinkupErrorResponse>(cancellationToken);
            if (errorResponse != null)
            {
                throw new LinkupException(errorResponse);
            }
            else
            {
                throw new LinkupException((int)response.StatusCode, $"API request failed with status code {response.StatusCode}");
            }
        }
        return await response.Content.ReadFromJsonAsync<FetchResponse>(cancellationToken) ?? new FetchResponse();

    }

    /// <summary>
    /// Gets the current balance/credits for the account
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>Balance response with the current credit balance</returns>
    /// <exception cref="LinkupException">Thrown when the API returns an error response with structured error information</exception>
    public async Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(_config.BalanceEndpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // For errors, we need the raw content to deserialize as LinkupErrorResponse
            var errorResponse = await response.Content.ReadFromJsonAsync<LinkupErrorResponse>(cancellationToken);
            if (errorResponse != null)
            {
                throw new LinkupException(errorResponse);
            }
            else
            {
                throw new LinkupException((int)response.StatusCode, $"API request failed with status code {response.StatusCode}");
            }
        }
        return await response.Content.ReadFromJsonAsync<BalanceResponse>(cancellationToken) ?? new BalanceResponse();
    }

}