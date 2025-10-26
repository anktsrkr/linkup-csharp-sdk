using System.ComponentModel;
using System.Text.Json;
using LinkupSdk.Client;
using LinkupSdk.Models;
using Microsoft.Extensions.AI;
#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace LinkupSdk.AITools;

/// <summary>
/// Provides AI tools for Linkup SDK operations that can be used with Microsoft.Extensions.AI.
/// These tools enable AI agents to perform web searches, content fetching, and balance inquiries programmatically.
/// The class offers both standard tools and approval-required versions for scenarios requiring human consent before execution.
/// </summary>
/// <remarks>
/// The LinkupTools class provides four main AI tools:
/// 1. SearchTool - Performs web searches with customizable parameters
/// 2. SearchStructuredTool - Performs structured web searches with JSON schema validation
/// 3. FetchContentTool - Fetches content from URLs with options for JavaScript rendering and image extraction
/// 4. GetBalanceTool - Retrieves account balance information
///
/// Each tool has an approval-required variant that ensures explicit human consent before execution.
/// </remarks>
public class LinkupTools
{
    private readonly LinkupClient _client;
    private readonly List<AITool> _tools;
    private readonly List<AITool> _approvalTools;

    /// <summary>
    /// AI tool for performing web searches without requiring approval
    /// </summary>
    public AITool SearchTool { get; private set; }

    /// <summary>
    /// AI tool for performing structured web searches without requiring approval
    /// </summary>
    public AITool SearchStructuredTool { get; private set; }

    /// <summary>
    /// AI tool for fetching content from URLs without requiring approval
    /// </summary>
    public AITool FetchContentTool { get; private set; }

    /// <summary>
    /// AI tool for getting account balance without requiring approval
    /// </summary>
    public AITool GetBalanceTool { get; private set; }

    /// <summary>
    /// AI tool for performing web searches that requires explicit human approval before execution
    /// </summary>
    public AITool SearchToolRequiringApproval { get; private set; }

    /// <summary>
    /// AI tool for performing structured web searches that requires explicit human approval before execution
    /// </summary>
    public AITool SearchStructuredToolRequiringApproval { get; private set; }

    /// <summary>
    /// AI tool for fetching content from URLs that requires explicit human approval before execution
    /// </summary>
    public AITool FetchContentToolRequiringApproval { get; private set; }

    /// <summary>
    /// AI tool for getting account balance that requires explicit human approval before execution
    /// </summary>
    public AITool GetBalanceToolRequiringApproval { get; private set; }

    public LinkupTools(LinkupClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));

        var _searchTool = AIFunctionFactory.Create(SearchAsync);
        var _searchStructuredTool = AIFunctionFactory.Create(SearchStructuredAsync);
        var _fetchContentTool = AIFunctionFactory.Create(FetchContentAsync);
        var _getBalanceTool = AIFunctionFactory.Create(GetBalanceAsync);

        SearchTool = _searchTool;
        SearchStructuredTool = _searchStructuredTool;
        FetchContentTool = _fetchContentTool;
        GetBalanceTool = _getBalanceTool;
        SearchToolRequiringApproval = new ApprovalRequiredAIFunction(_searchTool);
        SearchStructuredToolRequiringApproval = new ApprovalRequiredAIFunction(_searchStructuredTool);
        FetchContentToolRequiringApproval = new ApprovalRequiredAIFunction(_fetchContentTool);
        GetBalanceToolRequiringApproval = new ApprovalRequiredAIFunction(_getBalanceTool);

        _tools = [SearchTool, SearchStructuredTool, FetchContentTool, GetBalanceTool];
        _approvalTools = [SearchToolRequiringApproval, SearchStructuredToolRequiringApproval, FetchContentToolRequiringApproval, GetBalanceToolRequiringApproval];
    }

    public List<AITool> GetAllTools() => _tools;

    public List<AITool> GetAllToolsRequiringApproval() => _approvalTools;

    /// <summary>
    /// Performs a web search using the Linkup API
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="depth">The depth of the search (standard or deep)</param>
    /// <param name="outputType">The type of output to return</param>
    /// <param name="includeImages">Whether to include images in results</param>
    /// <param name="includeDomains">Domains to include in search</param>
    /// <param name="excludeDomains">Domains to exclude from search</param>
    /// <param name="fromDate">Start date for date range filtering (YYYY-MM-DD)</param>
    /// <param name="toDate">End date for date range filtering (YYYY-MM-DD)</param>
    /// <param name="includeInlineCitations">Whether to include inline citations for sourced answers</param>
    /// <returns>Search results as a formatted string</returns>
    [Description("Performs a web search using the Linkup API with customizable parameters")]
    public async Task<string> SearchAsync(
        [Description("The search query")] string query,
        [Description("Search depth: standard or deep")] string depth = "standard",
        [Description("Output type: searchResults, sourcedAnswer, or structured")] string outputType = "searchResults",
        [Description("Include images in results")] bool includeImages = false,
        [Description("Domains to include (comma-separated)")] string? includeDomains = null,
        [Description("Domains to exclude (comma-separated)")] string? excludeDomains = null,
        [Description("Start date for filtering (YYYY-MM-DD)")] string? fromDate = null,
        [Description("End date for filtering (YYYY-MM-DD)")] string? toDate = null,
        [Description("Include inline citations for sourced answers")] bool includeInlineCitations = false)
    {
        var searchDepth = Enum.Parse<SearchDepth>(depth, true);
        var searchOutputType = Enum.Parse<OutputType>(outputType, true);

        var request = new SearchRequest
        {
            Query = query,
            Depth = searchDepth,
            OutputType = searchOutputType,
            IncludeImages = includeImages,
            IncludeDomains = includeDomains?.Split(',').Select(d => d.Trim()).ToArray(),
            ExcludeDomains = excludeDomains?.Split(',').Select(d => d.Trim()).ToArray(),
            FromDate = fromDate,
            ToDate = toDate,
            IncludeInlineCitations = includeInlineCitations
        };

        try
        {
            var response = await _client.SearchAsync(request);

            return response switch
            {
                SearchResultsResponse results => FormatSearchResults(results),
                SourcedAnswerResponse answer => FormatSourcedAnswer(answer),
                _ => $"Search completed successfully. Response type: {response.GetType().Name}"
            };
        }
        catch (LinkupException ex)
        {
            return $"Search failed: {ex.Message}. Recovery suggestion: {ex.RecoverySuggestion}";
        }
        catch (Exception ex)
        {
            return $"Search failed with unexpected error: {ex.Message}";
        }
    }

    /// <summary>
    /// Performs a structured search using the Linkup API
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="structuredSchema">JSON schema for structured output</param>
    /// <param name="depth">The depth of the search (standard or deep)</param>
    /// <param name="includeSources">Whether to include sources in the response</param>
    /// <param name="includeImages">Whether to include images in results</param>
    /// <param name="includeDomains">Domains to include in search</param>
    /// <param name="excludeDomains">Domains to exclude from search</param>
    /// <param name="fromDate">Start date for date range filtering (YYYY-MM-DD)</param>
    /// <param name="toDate">End date for filtering (YYYY-MM-DD)</param>
    /// <returns>Structured search results as JSON string</returns>
    [Description("Performs a structured web search using the Linkup API with JSON schema validation")]
    public async Task<string> SearchStructuredAsync(
        [Description("The search query")] string query,
        [Description("JSON schema for structured output")] string structuredSchema,
        [Description("Search depth: standard or deep")] string depth = "standard",
        [Description("Include sources in response")] bool includeSources = false,
        [Description("Include images in results")] bool includeImages = false,
        [Description("Domains to include (comma-separated)")] string? includeDomains = null,
        [Description("Domains to exclude (comma-separated)")] string? excludeDomains = null,
        [Description("Start date for filtering (YYYY-MM-DD)")] string? fromDate = null,
        [Description("End date for filtering (YYYY-MM-DD)")] string? toDate = null)
    {
        var searchDepth = Enum.Parse<SearchDepth>(depth, true);

        var request = new SearchRequest
        {
            Query = query,
            Depth = searchDepth,
            OutputType = OutputType.structured,
            IncludeSources = includeSources,
            IncludeImages = includeImages,
            IncludeDomains = includeDomains?.Split(',').Select(d => d.Trim()).ToArray(),
            ExcludeDomains = excludeDomains?.Split(',').Select(d => d.Trim()).ToArray(),
            FromDate = fromDate,
            ToDate = toDate,
            StructuredOutputSchema = structuredSchema
        };

        try
        {
            // For structured search, we need to use the generic method with a dynamic type
            // Since we don't know the type at compile time, we'll use JsonElement
            var response = await _client.SearchAsync(request);

            if (response is StructuredResponseWithSources structuredWithSources)
            {
                return JsonSerializer.Serialize(structuredWithSources.Data, _cachedJsonSerializerOptions);
            }
            else if (response is StructuredResponse structured)
            {
                return JsonSerializer.Serialize(structured.Data, _cachedJsonSerializerOptions);
            }
            else
            {
                return "Structured search completed but response type was unexpected";
            }
        }
        catch (LinkupException ex)
        {
            return $"Structured search failed: {ex.Message}. Recovery suggestion: {ex.RecoverySuggestion}";
        }
        catch (Exception ex)
        {
            return $"Structured search failed with unexpected error: {ex.Message}";
        }
    }

    /// <summary>
    /// Fetches content from a URL using the Linkup API
    /// </summary>
    /// <param name="url">The URL to fetch content from</param>
    /// <param name="renderJs">Whether to render JavaScript on the page</param>
    /// <param name="includeRawHtml">Whether to include raw HTML in the response</param>
    /// <param name="extractImages">Whether to extract images from the page</param>
    /// <returns>Fetched content as formatted text</returns>
    [Description("Fetches content from a URL using the Linkup API")]
    public async Task<string> FetchContentAsync(
        [Description("The URL to fetch content from")] string url,
        [Description("Render JavaScript on the page")] bool renderJs = false,
        [Description("Include raw HTML in response")] bool includeRawHtml = false,
        [Description("Extract images from the page")] bool extractImages = false)
    {
        var request = new FetchRequest
        {
            Url = url,
            RenderJs = renderJs,
            IncludeRawHtml = includeRawHtml,
            ExtractImages = extractImages
        };

        try
        {
            var response = await _client.FetchAsync(request);

            var result = $"Content from {url}:\n\n{response.Markdown}";

            if (response.Images?.Length > 0)
            {
                result += "\n\nImages found:\n" + string.Join("\n", response.Images.Select(img => $"- {img.Alt}: {img.Url}"));
            }

            if (includeRawHtml && !string.IsNullOrEmpty(response.RawHtml))
            {
                result += $"\n\nRaw HTML length: {response.RawHtml.Length} characters";
            }

            return result;
        }
        catch (LinkupException ex)
        {
            return $"Fetch failed: {ex.Message}. Recovery suggestion: {ex.RecoverySuggestion}";
        }
        catch (Exception ex)
        {
            return $"Fetch failed with unexpected error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the current account balance/credits
    /// </summary>
    /// <returns>Current balance information</returns>
    [Description("Gets the current account balance/credits from the Linkup API")]
    public async Task<string> GetBalanceAsync()
    {
        try
        {
            var response = await _client.GetBalanceAsync();
            return $"Current balance: {response.Balance}�";
        }
        catch (LinkupException ex)
        {
            return $"Balance check failed: {ex.Message}. Recovery suggestion: {ex.RecoverySuggestion}";
        }
        catch (Exception ex)
        {
            return $"Balance check failed with unexpected error: {ex.Message}";
        }
    }

    private static string FormatSearchResults(SearchResultsResponse response)
    {
        if (response.Results == null || response.Results.Length == 0)
        {
            return "No search results found.";
        }

        var result = $"Found {response.Results.Length} results:\n\n";

        foreach (var item in response.Results)
        {
            result += $"- **{item.Name}**\n";
            result += $"  URL: {item.Url}\n";

            if (item is TextSearchResult textResult)
            {
                result += $"  Content: {textResult.Content}\n";
            }

            result += "\n";
        }

        return result;
    }

    private static string FormatSourcedAnswer(SourcedAnswerResponse response)
    {
        var result = $"Answer: {response.Answer}\n\n";

        if (response.Sources?.Length > 0)
        {
            result += "Sources:\n";
            foreach (var source in response.Sources)
            {
                result += $"- {source.Name}: {source.Url}\n";
                result += $"  Snippet: {source.Snippet}\n\n";
            }
        }

        return result;
    }

    private static readonly JsonSerializerOptions _cachedJsonSerializerOptions = new()
    {
        WriteIndented = false
    };
}