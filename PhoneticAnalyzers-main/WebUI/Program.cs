using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using PhoneticAnalyzers.WebUI;
using PhoneticAnalyzers.WebUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure base URLs
var ingestionBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:7071";
var searchBaseUrl = builder.Configuration["SearchApiSettings:BaseUrl"] ?? "http://localhost:7072";

// Default HttpClient targets ingestion API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(ingestionBaseUrl) });

// Register MudBlazor services
builder.Services.AddMudServices();

// Register application services
builder.Services.AddScoped<IIngestionApiClient, IngestionApiClient>();
builder.Services.AddScoped<ISearchApiClient>(sp =>
{
	var logger = sp.GetRequiredService<ILogger<SearchApiClient>>();
	return new SearchApiClient(new HttpClient { BaseAddress = new Uri(searchBaseUrl) }, logger);
});
builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddSingleton<SearchStateService>();

await builder.Build().RunAsync();
