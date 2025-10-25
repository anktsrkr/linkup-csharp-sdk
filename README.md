# LinkupSdk

A C# SDK for interacting with the Linkup API, providing easy-to-use methods for searching and fetching data.

## Features

- Search functionality
- Fetch operations
- Balance inquiries
- Built-in resilience and error handling

## Installation

Install the package via NuGet:

```
dotnet add package LinkupSdk
```

## Usage

```csharp
using LinkupSdk;

// Configure the client
var config = new LinkupConfig
{
    ApiKey = "your-api-key",
    BaseUrl = "https://api.linkup.com"
};

var client = new LinkupClient(config);

// Example: Search
var searchRequest = new SearchRequest { Query = "example" };
var searchResponse = await client.SearchAsync(searchRequest);
```

## Requirements

- .NET 8.0 or later

## License

MIT