using LinkupSdk.AITools;
using LinkupSdk.Client;
using LinkupSdk.Extensions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var endPoint = configuration["OpenAI:Endpoint"] ?? throw new InvalidOperationException("OpenAI:Endpoint is not set in appsettings.json.");
var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not set in appsettings.json.");
var model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

// Setup Linkup client
var linkupApiKey = configuration["Linkup:ApiKey"] ?? throw new InvalidOperationException("Linkup:ApiKey is not set in appsettings.json.");


var services = new ServiceCollection();
services.AddLinkupClient(config => config.ApiKey = linkupApiKey);
services.AddSingleton<LinkupTools>();
var serviceProvider = services.BuildServiceProvider();
var linkupClient = serviceProvider.GetRequiredService<LinkupClient>();
var linkupTools = serviceProvider.GetRequiredService<LinkupTools>();

AIAgent agent = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endPoint) })
         .GetChatClient(model)
         .CreateAIAgent(instructions: "You are an AI assistant equipped with Linkup SDK tools for performing web searches, structured searches, fetching content from URLs, and checking account balance.",
          name: "LinkupAssistant", tools: [linkupTools.SearchToolRequiringApproval]);

UserChatMessage chatMessage = new("What is my current account balance?");

// Invoke the agent and output the text result.
ChatCompletion chatCompletion = await agent.RunAsync([chatMessage]);


Console.WriteLine(chatCompletion.Content.Last().Text);