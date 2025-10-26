using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http.Resilience;
using LinkupSdk.Client;
using LinkupSdk.Configuration;

namespace LinkupSdk.Extensions;

/// <summary>
/// Extension methods for LinkupClient
/// </summary>
public static class LinkupClientExtensions
{
    private const string DefaultBaseUrl = "https://api.linkup.so/v1/";
    private const string UserAgent = "Linkup-CSharp-SDK/1.0.0";
    /// <summary>
    /// Adds the LinkupClient to the service collection with proper HttpClient configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="config">The API configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLinkupClient(this IServiceCollection services, LinkupConfig config)
    {
        ArgumentException.ThrowIfNullOrEmpty(config.ApiKey, nameof(config.ApiKey));

        services.Configure<LinkupConfig>(options =>
        {
            options.ApiKey = config.ApiKey;
            options.BaseUrl = config.BaseUrl;
        });
        services.AddHttpClient<LinkupClient>(ConfigureHttpClient)
            .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// Adds the LinkupClient to the service collection with configuration using an action
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure the ApiConfig</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLinkupClient(this IServiceCollection services, Action<LinkupConfig> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddHttpClient<LinkupClient>(ConfigureHttpClient)
        .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// Adds the LinkupClient to the service collection with configuration from a configuration section
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration to bind to</param>
    /// <param name="sectionName">The configuration section name (defaults to "Linkup")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLinkupClient(this IServiceCollection services, IConfiguration configuration, string sectionName = "Linkup")
    {
        services.Configure<LinkupConfig>(configuration.GetSection(sectionName));
        services.AddHttpClient<LinkupClient>(ConfigureHttpClient)
        .AddStandardResilienceHandler();

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider sp, HttpClient httpClient)
    {
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<LinkupConfig>>();
        var config = optionsMonitor.CurrentValue;
        var baseUrl = config.BaseUrl ?? DefaultBaseUrl;
        var userAgent = UserAgent;

        httpClient.BaseAddress = new Uri(baseUrl);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
}