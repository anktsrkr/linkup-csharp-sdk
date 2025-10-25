using LinkupSdk.Client;
using LinkupSdk.Configuration;
using LinkupSdk.Console.Example.Model;
using LinkupSdk.Extensions;
using LinkupSdk.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Globalization;
using System.Text.Json;


AnsiConsole.Write(
new FigletText("Linkup SDK Console")
    .Centered()
    .Color(Color.Blue));

AnsiConsole.MarkupLine("[bold green]Welcome to the Linkup SDK Console Example![/]");

// Ask user how they want to provide the API key
var apiKeySource = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("How would you like to provide your Linkup API key?")
        .AddChoices("From configuration file (appsettings.json)", "Enter manually"));

LinkupClient client;

if (apiKeySource == "From configuration file (appsettings.json)")
{
    // Load configuration from appsettings.json
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    var linkupConfig = configuration.GetSection("Linkup").Get<LinkupConfig>();
    if (linkupConfig?.ApiKey == null || linkupConfig.ApiKey == "your-api-key-here")
    {
        AnsiConsole.MarkupLine("[red]API key not found in appsettings.json. Please set it in the 'Linkup:ApiKey' section.[/]");
        return;
    }

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLinkupClient(configuration);
    var serviceProvider = services.BuildServiceProvider();
    client = serviceProvider.GetRequiredService<LinkupClient>();
}
else
{
    // Get API key from user input
    var apiKey = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter your Linkup API key:")
            .PromptStyle("red")
            .Secret());

    var services = new ServiceCollection();
    services.AddLinkupClient(config =>
    {
        config.ApiKey = apiKey;
    });
    var serviceProvider = services.BuildServiceProvider();
    client = serviceProvider.GetRequiredService<LinkupClient>();
}

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .PageSize(10)
            .AddChoices([
                "Search",
                        "Fetch URL",
                        "Get Balance",
                        "Example: Microsoft's 2024 Revenue",
                        "Exit"
            ]));

    switch (choice)
    {
        case "Search":
            await PerformSearchAsync(client);
            break;
        case "Fetch URL":
            await PerformFetchAsync(client);
            break;
        case "Get Balance":
            await GetBalanceAsync(client);
            break;
        case "Example: Microsoft's 2024 Revenue":
            await PerformMicrosoftRevenueExampleAsync(client);
            break;
        case "Exit":
            AnsiConsole.MarkupLine("[bold yellow]Goodbye![/]");
            return;
    }

    AnsiConsole.WriteLine();
}

async Task PerformSearchAsync(LinkupClient client)
{
    AnsiConsole.MarkupLine("[bold blue]Search Configuration[/]");

    var query = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter search query:")
            .Validate(q => !string.IsNullOrWhiteSpace(q), "Query cannot be empty"));

    var depth = AnsiConsole.Prompt(
        new SelectionPrompt<SearchDepth>()
            .Title("Select search depth:")
            .AddChoices(SearchDepth.standard, SearchDepth.deep));

    var outputType = AnsiConsole.Prompt(
        new SelectionPrompt<OutputType>()
            .Title("Select output type:")
            .AddChoices(OutputType.searchResults, OutputType.sourcedAnswer, OutputType.structured));

    var includeImages = AnsiConsole.Prompt(
        new ConfirmationPrompt("Include images in results?") { DefaultValue = false });

    var includeDomains = AnsiConsole.Prompt(
        new TextPrompt<string?>("Include specific domains (comma-separated, optional):")
            .AllowEmpty());

    var excludeDomains = AnsiConsole.Prompt(
        new TextPrompt<string?>("Exclude specific domains (comma-separated, optional):")
            .AllowEmpty());

    var fromDateStr = AnsiConsole.Prompt(
        new TextPrompt<string?>("From date (YYYY-MM-DD, optional):")
            .AllowEmpty());

    var toDateStr = AnsiConsole.Prompt(
        new TextPrompt<string?>("To date (YYYY-MM-DD, optional):")
            .AllowEmpty());

    DateTime? fromDate = null;
    if (!string.IsNullOrWhiteSpace(fromDateStr))
    {
        if (DateTime.TryParse(fromDateStr, out var date))
            fromDate = date;
        else
            AnsiConsole.MarkupLine("[red]Invalid from date format, ignoring.[/]");
    }

    DateTime? toDate = null;
    if (!string.IsNullOrWhiteSpace(toDateStr))
    {
        if (DateTime.TryParse(toDateStr, out var date))
            toDate = date;
        else
            AnsiConsole.MarkupLine("[red]Invalid to date format, ignoring.[/]");
    }

    bool? includeInlineCitations = outputType == OutputType.sourcedAnswer ?
        AnsiConsole.Prompt(new ConfirmationPrompt("Include inline citations?")) : null;

    bool? includeSources = outputType == OutputType.structured ?
        AnsiConsole.Prompt(new ConfirmationPrompt("Include sources?")) : null;

    object? structuredSchema = null;
    if (outputType == OutputType.structured)
    {
        var schemaInput = AnsiConsole.Prompt(
            new TextPrompt<string?>("Enter JSON schema for structured output (optional):")
                .AllowEmpty());
        if (!string.IsNullOrWhiteSpace(schemaInput))
        {
            try
            {
                structuredSchema = JsonSerializer.Deserialize<object>(schemaInput);
            }
            catch
            {
                AnsiConsole.MarkupLine("[red]Invalid JSON schema, proceeding without it.[/]");
            }
        }
    }

    var parameters = new SearchRequest
    {
        Query = query,
        Depth = depth,
        OutputType = outputType,
        IncludeImages = includeImages,
        IncludeDomains = string.IsNullOrWhiteSpace(includeDomains) ? null :
            [.. includeDomains.Split(',').Select(d => d.Trim())],
        ExcludeDomains = string.IsNullOrWhiteSpace(excludeDomains) ? null :
            [.. excludeDomains.Split(',').Select(d => d.Trim())],
        FromDate = fromDate?.ToString("O"),
        ToDate = toDate?.ToString("O"),
        IncludeInlineCitations = includeInlineCitations,
        IncludeSources = includeSources,
        StructuredOutputSchema = structuredSchema != null ? JsonSerializer.Serialize(structuredSchema) : null
    };

    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Searching...");
            task.IsIndeterminate = true;

            try
            {
                var response = await client.SearchAsync(parameters);
                task.StopTask();
                DisplaySearchResults(response, outputType);
            }
            catch (LinkupException ex)
            {
                task.StopTask();
                DisplayErrorDetails(ex);
            }
        });
}

async Task PerformFetchAsync(LinkupClient client)
{
    AnsiConsole.MarkupLine("[bold blue]Fetch Configuration[/]");

    var url = AnsiConsole.Prompt(
        new TextPrompt<string>("Enter URL to fetch:")
            .Validate(u => Uri.TryCreate(u, UriKind.Absolute, out _), "Please enter a valid URL"));

    var renderJs = AnsiConsole.Prompt(
        new ConfirmationPrompt("Render JavaScript?"));

    var includeRawHtml = AnsiConsole.Prompt(
        new ConfirmationPrompt("Include raw HTML?"));

    var extractImages = AnsiConsole.Prompt(
        new ConfirmationPrompt("Extract images?"));

    var parameters = new FetchRequest
    {
        Url = url,
        RenderJs = renderJs,
        IncludeRawHtml = includeRawHtml,
        ExtractImages = extractImages
    };

    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Fetching...");
            task.IsIndeterminate = true;

            try
            {
                var response = await client.FetchAsync(parameters);
                task.StopTask();
                DisplayFetchResults(response);
            }
            catch (LinkupException ex)
            {
                task.StopTask();
                DisplayErrorDetails(ex);
            }
        });
}

async Task GetBalanceAsync(LinkupClient client)
{
    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Getting balance...");
            task.IsIndeterminate = true;

            try
            {
                var response = await client.GetBalanceAsync();
                task.StopTask();
                AnsiConsole.MarkupLine($"[green]Credits: {response.Balance.ToString("C", CultureInfo.GetCultureInfo("fr-FR"))}[/]");
            }
            catch (LinkupException ex)
            {
                task.StopTask();
                DisplayErrorDetails(ex);
            }
        });
}

async Task PerformMicrosoftRevenueExampleAsync(LinkupClient client)
{

    var includeSources = AnsiConsole.Prompt(
        new ConfirmationPrompt("Include sources in the response?"));

    AnsiConsole.MarkupLine("[bold blue]Example: Microsoft's 2024 Revenue[/]");

    var parameters = new SearchRequest
    {
        Query = "What is Microsoft's 2024 revenue?",
        Depth = SearchDepth.deep,
        OutputType = OutputType.structured,
        IncludeSources = includeSources,
    };

    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Searching for Microsoft's 2024 revenue...");
            task.IsIndeterminate = true;

            try
            {
                // Use the generic SearchStructuredAsync method for typed response
                var response = await client.SearchAsync<RevenueData>(parameters);
                task.StopTask();

                // Display the typed data directly
                if (response is StructuredResponseWithSources<RevenueData> structuredWithSources && structuredWithSources.Data != null)
                {
                    AnsiConsole.MarkupLine("[bold]Revenue Data:[/]");
                    AnsiConsole.WriteLine($"Company Name: {structuredWithSources.Data.CompanyName}");
                    AnsiConsole.WriteLine($"Revenue Amount: {structuredWithSources.Data.RevenueAmount:C}");
                    AnsiConsole.WriteLine($"Fiscal Year: {structuredWithSources.Data.FiscalYear}");

                    if (structuredWithSources.Sources != null && structuredWithSources.Sources.Length > 0)
                    {
                        AnsiConsole.MarkupLine("[bold]Sources:[/]");
                        var sourcesTable = new Table();
                        sourcesTable.AddColumn("Name");
                        sourcesTable.AddColumn("URL");
                        sourcesTable.AddColumn("Snippet");

                        foreach (var source in structuredWithSources.Sources)
                        {
                            var snippet = source is TextSearchResult textSource ? textSource.Content : "N/A";
                            sourcesTable.AddRow(source.Name, source.Url, Markup.Escape( snippet[..Math.Min(50, snippet.Length)]));
                        }
                        AnsiConsole.Write(sourcesTable);
                    }
                }
                else if (response is StructuredResponse<RevenueData> structured && structured.Data != null)
                {
                    AnsiConsole.MarkupLine("[bold]Revenue Data:[/]");
                    AnsiConsole.WriteLine($"Company Name: {structured.Data.CompanyName}");
                    AnsiConsole.WriteLine($"Revenue Amount: {structured.Data.RevenueAmount:C}");
                    AnsiConsole.WriteLine($"Fiscal Year: {structured.Data.FiscalYear}");
                }
            }
            catch (LinkupException ex)
            {
                task.StopTask();
                DisplayErrorDetails(ex);
            }
        });
}

void DisplaySearchResults(SearchResponse response, OutputType outputType)
{
    switch (outputType)
    {
        case OutputType.searchResults:
            if (response is SearchResultsResponse searchResponse && searchResponse.Results != null)
            {
                var table = new Table();
                table.AddColumn("Type");
                table.AddColumn("Name");
                table.AddColumn("URL");

                foreach (var result in searchResponse.Results)
                {
                    var type = result switch
                    {
                        TextSearchResult => "text",
                        ImageSearchResult => "image",
                        _ => "unknown"
                    };
                    table.AddRow(type, result.Name, result.Url);
                    if (result is TextSearchResult textResult)
                    {
                        table.AddRow("", "[dim]" + textResult.Content[..Math.Min(100, textResult.Content.Length)] + "...[/]", "");
                    }
                }
                AnsiConsole.Write(table);
            }
            break;

        case OutputType.sourcedAnswer:
            if (response is SourcedAnswerResponse answerResponse)
            {
                if (!string.IsNullOrEmpty(answerResponse.Answer))
                {
                    AnsiConsole.MarkupLine("[bold]Answer:[/]");
                    AnsiConsole.WriteLine(answerResponse.Answer);
                }
                if (answerResponse.Sources != null)
                {
                    AnsiConsole.MarkupLine("[bold]Sources:[/]");
                    var sourcesTable = new Table();
                    sourcesTable.AddColumn("Name");
                    sourcesTable.AddColumn("URL");
                    sourcesTable.AddColumn("Snippet");

                    foreach (var source in answerResponse.Sources)
                    {
                        sourcesTable.AddRow(source.Name, source.Url, Markup.Escape( source.Snippet[..Math.Min(50, source.Snippet.Length)]));
                    }
                    AnsiConsole.Write(sourcesTable);
                }
            }
            break;

        case OutputType.structured:
            if (response is StructuredResponseWithSources<object> structuredWithSourcesResponse && structuredWithSourcesResponse.Data != null)
            {
                // Try to deserialize to RevenueData for the example
                try
                {
                    var revenueData = JsonSerializer.Deserialize<RevenueData>(JsonSerializer.Serialize(structuredWithSourcesResponse.Data), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (revenueData != null)
                    {
                        AnsiConsole.MarkupLine("[bold]Revenue Data:[/]");
                        AnsiConsole.WriteLine($"Company Name: {revenueData.CompanyName}");
                        AnsiConsole.WriteLine($"Revenue Amount: {revenueData.RevenueAmount:C}");
                        AnsiConsole.WriteLine($"Fiscal Year: {revenueData.FiscalYear}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[bold]Structured Data:[/]");
                        AnsiConsole.WriteLine(JsonSerializer.Serialize(structuredWithSourcesResponse.Data, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
                catch
                {
                    AnsiConsole.MarkupLine("[bold]Structured Data:[/]");
                    AnsiConsole.WriteLine(JsonSerializer.Serialize(structuredWithSourcesResponse.Data, new JsonSerializerOptions { WriteIndented = true }));
                }

                if (structuredWithSourcesResponse.Sources != null && structuredWithSourcesResponse.Sources.Length > 0)
                {
                    AnsiConsole.MarkupLine("[bold]Sources:[/]");
                    var sourcesTable = new Table();
                    sourcesTable.AddColumn("Name");
                    sourcesTable.AddColumn("URL");
                    sourcesTable.AddColumn("Snippet");

                    foreach (var source in structuredWithSourcesResponse.Sources)
                    {
                        var snippet = source is TextSearchResult textSource ? textSource.Content : "N/A";
                        sourcesTable.AddRow(source.Name, source.Url,Markup.Escape( snippet[..Math.Min(50, snippet.Length)]));
                    }
                    AnsiConsole.Write(sourcesTable);
                }
            }
            else if (response is StructuredResponse<object> structuredResponse && structuredResponse.Data != null)
            {
                AnsiConsole.MarkupLine("[bold]Structured Data:[/]");
                AnsiConsole.WriteLine(JsonSerializer.Serialize(structuredResponse.Data, new JsonSerializerOptions { WriteIndented = true }));
            }
            break;
    }
}

void DisplayErrorDetails(LinkupException ex)
{
    AnsiConsole.MarkupLine($"[red]Status Code: {ex.StatusCode}[/]");
    AnsiConsole.MarkupLine($"[red]Error Code: {ex.ErrorCode}[/]");
    AnsiConsole.MarkupLine($"[red]Message: {ex.Message}[/]");

    if (ex.ErrorDetails != null && ex.ErrorDetails.Count > 0)
    {
        AnsiConsole.MarkupLine("[red]Details:[/]");
        foreach (var detail in ex.ErrorDetails)
        {
            AnsiConsole.MarkupLine($"[red]  Field: {detail.Field}, Message: {detail.Message}[/]");
        }
    }

    if (!string.IsNullOrEmpty(ex.RecoverySuggestion))
    {
        AnsiConsole.MarkupLine($"[yellow]Recovery Suggestion: {ex.RecoverySuggestion}[/]");
    }
}

void DisplayFetchResults(FetchResponse response)
{
    if (!string.IsNullOrEmpty(response.Markdown))
    {
        AnsiConsole.MarkupLine("[bold]Markdown:[/]");
        AnsiConsole.WriteLine(Markup.Escape(response.Markdown));
    }

    if (!string.IsNullOrEmpty(response.RawHtml))
    {
        AnsiConsole.MarkupLine("[bold]Raw HTML (first 500 chars):[/]");
        AnsiConsole.WriteLine(Markup.Escape(response.RawHtml[..Math.Min(500, response.RawHtml.Length)]));
    }

    if (response.Images != null && response.Images.Length > 0)
    {
        AnsiConsole.MarkupLine("[bold]Images:[/]");
        foreach (var image in response.Images)
        {
            AnsiConsole.WriteLine($"Alt: {image.Alt}, URL: {image.Url}");
        }
    }
}
