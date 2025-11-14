using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PhoneticAnalyzers.WebUI.Services;

public interface IApiClientFactory
{
    IIngestionApiClient CreateIngestionClient(IServiceProvider sp);
    ISearchApiClient CreateSearchClient(IServiceProvider sp);
}

public class ApiClientFactory : IApiClientFactory
{
    private readonly IConfiguration _config;
    public ApiClientFactory(IConfiguration config)
    {
        _config = config;
    }

    public IIngestionApiClient CreateIngestionClient(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILogger<IngestionApiClient>>();
        var approach = _config["ApiSettings:Approach"] ?? "CodeFirst";
        var baseUrl = _config[$"ApiSettings:{approach}:IngestionBaseUrl"] ?? "http://localhost:7071/api";
        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        return new IngestionApiClient(httpClient, logger);
    }

    public ISearchApiClient CreateSearchClient(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILogger<SearchApiClient>>();
        var approach = _config["ApiSettings:Approach"] ?? "CodeFirst";
        var baseUrl = _config[$"ApiSettings:{approach}:SearchBaseUrl"] ?? "http://localhost:7072/api";
        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        return new SearchApiClient(httpClient, logger);
    }
}
