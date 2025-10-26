# LinkupSdk

A C# SDK for interacting with the Linkup API, providing easy-to-use methods for searching and fetching data.

## Features

- Search functionality with multiple output types (search results, sourced answers, structured data)
- Fetch operations with JavaScript rendering and HTML extraction
- Balance inquiries
- Built-in resilience and error handling
- Support for dependency injection
- Advanced search options (depth, date ranges, domain filtering)

## Installation

Install the package via NuGet:

```
dotnet add package LinkupSdk
```

## Configuration

The SDK supports two main configuration approaches:

### 1. Using appsettings.json

Add your API key to your `appsettings.json` file:

```json
{
  "Linkup": {
    "ApiKey": "your-api-key-here"
  }
}
```

Then register the client with dependency injection:

```csharp
using LinkupSdk.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// In your startup code
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLinkupClient(configuration);
var serviceProvider = services.BuildServiceProvider();

var client = serviceProvider.GetRequiredService<LinkupClient>();
```

### 2. Manual Configuration

Configure the client directly with your API key:

```csharp
using LinkupSdk.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLinkupClient(config =>
{
    config.ApiKey = "your-api-key";
});
var serviceProvider = services.BuildServiceProvider();

var client = serviceProvider.GetRequiredService<LinkupClient>();
```

## Usage

### Search Operations

The SDK supports different search depths and output types:

```csharp
using LinkupSdk;
using LinkupSdk.Models;

// Basic search
var searchRequest = new SearchRequest 
{ 
    Query = "example search query",
    Depth = SearchDepth.standard
};
var searchResponse = await client.SearchAsync(searchRequest);

// Advanced search with filters
var advancedSearchRequest = new SearchRequest
{
    Query = "example with filters",
    Depth = SearchDepth.deep,
    OutputType = OutputType.sourcedAnswer,
    IncludeImages = true,
    IncludeDomains = new[] { "example.com", "another-example.com" },
    ExcludeDomains = new[] { "exclude.com" },
    FromDate = "2023-01-01",
    ToDate = "2023-12-31",
    IncludeInlineCitations = true
};
var advancedResponse = await client.SearchAsync(advancedSearchRequest);

// Structured search with custom schema
var structuredRequest = new SearchRequest
{
    Query = "structured data query",
    OutputType = OutputType.structured,
    StructuredOutputSchema = @"{
        ""type"": ""object"",
        ""properties"": {
            ""title"": {""type"": ""string""},
            ""summary"": {""type"": ""string""}
        }
    }"
};
var structuredResponse = await client.SearchAsync(structuredRequest);
```

### Fetch Operations

Fetch web content with various options:

```csharp
using LinkupSdk.Models;

var fetchRequest = new FetchRequest
{
    Url = "https://example.com",
    RenderJs = true,           // Render JavaScript content
    IncludeRawHtml = true,     // Include raw HTML in response
    ExtractImages = true       // Extract image information
};
var fetchResponse = await client.FetchAsync(fetchRequest);

Console.WriteLine($"Markdown content: {fetchResponse.Markdown}");
Console.WriteLine($"Raw HTML: {fetchResponse.RawHtml?.Substring(0, 500)}...");
foreach (var image in fetchResponse.Images)
{
    Console.WriteLine($"Image: {image.Alt} - {image.Url}");
}
```

### Balance Inquiry

Check your account balance:

```csharp
var balanceResponse = await client.GetBalanceAsync();
Console.WriteLine($"Credits: {balanceResponse.Balance:C}");
```

### Typed Structured Responses

For structured data queries with known schemas, you can use generic methods:

```csharp
// Define your response model
public class RevenueData
{
    public string CompanyName { get; set; }
    public decimal RevenueAmount { get; set; }
    public int FiscalYear { get; set; }
}

// Perform a structured search with a typed response
var parameters = new SearchRequest
{
    Query = "What is Microsoft's 2024 revenue?",
    Depth = SearchDepth.deep,
    OutputType = OutputType.structured,
    IncludeSources = true,
};

var response = await client.SearchAsync<RevenueData>(parameters);

if (response is StructuredResponseWithSources<RevenueData> structuredWithSources && 
    structuredWithSources.Data != null)
{
    Console.WriteLine($"Company: {structuredWithSources.Data.CompanyName}");
    Console.WriteLine($"Revenue: {structuredWithSources.Data.RevenueAmount:C}");
    Console.WriteLine($"Year: {structuredWithSources.Data.FiscalYear}");
    
    // Access sources if included
    if (structuredWithSources.Sources != null)
    {
        foreach (var source in structuredWithSources.Sources)
        {
            Console.WriteLine($"Source: {source.Name} - {source.Url}");
        }
    }
}
```

## Console Example

The repository includes a comprehensive console example that demonstrates all SDK features:

```bash
# Navigate to the example directory
cd samples/LinkupSdk.Console.Example

# Run the example
dotnet run
```

The console example provides an interactive interface for:
- Performing different types of searches
- Fetching web content with various options
- Checking account balance
- Executing structured data queries with typed responses

## Error Handling

The SDK provides detailed error information:

```csharp
try
{
    var response = await client.SearchAsync(searchRequest);
}
catch (LinkupException ex)
{
    Console.WriteLine($"Status Code: {ex.StatusCode}");
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
    Console.WriteLine($"Message: {ex.Message}");
    
    if (ex.ErrorDetails != null)
    {
        foreach (var detail in ex.ErrorDetails)
        {
            Console.WriteLine($"Field: {detail.Field}, Message: {detail.Message}");
        }
    }
    
    if (!string.IsNullOrEmpty(ex.RecoverySuggestion))
    {
        Console.WriteLine($"Recovery Suggestion: {ex.RecoverySuggestion}");
    }
}
```

## AI Tools

The LinkupSdk includes AI tools that can be used with Microsoft.Extensions.AI for building AI-powered applications. These tools provide a bridge between AI models and the Linkup API, allowing AI agents to perform web searches, content fetching, and balance inquiries programmatically.

### Installation

To use the AI tools, you can use same package via NuGet:

```
dotnet add package LinkupSdk
```

### Usage with Microsoft.Extensions.AI

```csharp
using LinkupSdk.AITools;
using LinkupSdk.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection
var services = new ServiceCollection();
services.AddLinkupClient(config =>
{
    config.ApiKey = "your-api-key";
});
var serviceProvider = services.BuildServiceProvider();

// Create the LinkupTools instance
var client = serviceProvider.GetRequiredService<LinkupClient>();
var linkupTools = new LinkupTools(client);

// Use with an AI model (example with Azure OpenAI)
var chatClient = new AzureOpenAIChatClient(new Uri("https://your-resource.openai.azure.com/"), new AzureKeyCredential("your-key"));
chatClient = chatClient.WithTools(linkupTools.GetAllTools());

// Execute chat with tools
var response = await chatClient.CompleteAsync("Search for the latest developments in renewable energy.");
Console.WriteLine(response.Message.Text);
```

### Available Tools

The LinkupTools class provides the following AI tools for different operations:

#### Search Tool
- **Function**: `SearchAsync`
- **Description**: Performs a web search using the Linkup API with customizable parameters
- **Parameters**:
  - `query`: The search query
  - `depth`: Search depth (standard or deep)
  - `outputType`: Output type (searchResults, sourcedAnswer, or structured)
  - `includeImages`: Whether to include images in results
  - `includeDomains`: Domains to include in search (comma-separated)
  - `excludeDomains`: Domains to exclude from search (comma-separated)
  - `fromDate`: Start date for date range filtering (YYYY-MM-DD)
  - `toDate`: End date for date range filtering (YYYY-MM-DD)
  - `includeInlineCitations`: Whether to include inline citations for sourced answers

#### Structured Search Tool
- **Function**: `SearchStructuredAsync`
- **Description**: Performs a structured web search using the Linkup API with JSON schema validation
- **Parameters**:
  - `query`: The search query
  - `structuredSchema`: JSON schema for structured output
  - `depth`: Search depth (standard or deep)
  - `includeSources`: Whether to include sources in the response
  - `includeImages`: Whether to include images in results
  - `includeDomains`: Domains to include in search (comma-separated)
  - `excludeDomains`: Domains to exclude from search (comma-separated)
  - `fromDate`: Start date for date range filtering (YYYY-MM-DD)
  - `toDate`: End date for date range filtering (YYYY-MM-DD)

#### Fetch Content Tool
- **Function**: `FetchContentAsync`
- **Description**: Fetches content from a URL using the Linkup API
- **Parameters**:
  - `url`: The URL to fetch content from
  - `renderJs`: Whether to render JavaScript on the page
  - `includeRawHtml`: Whether to include raw HTML in the response
  - `extractImages`: Whether to extract images from the page

#### Get Balance Tool
- **Function**: `GetBalanceAsync`
- **Description**: Gets the current account balance/credits from the Linkup API
- **Parameters**: None

### Approval-Required Tools

For scenarios requiring explicit human approval before executing operations, LinkupTools provides approval-required versions of all tools. These tools wrap the standard tools with an approval mechanism that ensures human consent before execution. To access these tools, use the `GetAllToolsRequiringApproval()` method instead of `GetAllTools()`.

## Requirements

- .NET 8.0 or later

## Upcoming Features

- MCP support using C# MCP SDK

## License

MIT