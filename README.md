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

## Requirements

- .NET 8.0 or later

## License

MIT