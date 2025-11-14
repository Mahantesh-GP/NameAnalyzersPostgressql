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


// Register MudBlazor services
builder.Services.AddMudServices();

// Register ApiClientFactory for dual-mode backend selection
builder.Services.AddSingleton<IApiClientFactory, ApiClientFactory>();

// Register application services using the factory
builder.Services.AddScoped<IIngestionApiClient>(sp =>
{
	var factory = sp.GetRequiredService<IApiClientFactory>();
	return factory.CreateIngestionClient(sp);
});
builder.Services.AddScoped<ISearchApiClient>(sp =>
{
	var factory = sp.GetRequiredService<IApiClientFactory>();
	return factory.CreateSearchClient(sp);
});
builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddSingleton<SearchStateService>();

await builder.Build().RunAsync();
