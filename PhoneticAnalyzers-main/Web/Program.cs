using PhoneticAnalyzers.Web.Components;
using PhoneticAnalyzers.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Enable detailed circuit errors during development to diagnose issues
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

// Configure HTTP clients for API services
builder.Services.AddHttpClient("IngestionApi", client =>
{
    // Configure base address for the Ingestion API
    var apiBaseAddress = builder.Configuration["ApiSettings:BaseAddress"] ?? "http://localhost:7071";
    client.BaseAddress = new Uri(apiBaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("SearchApi", client =>
{
    // Configure base address for the Search API
    var searchApiBaseAddress = builder.Configuration["ApiSettings:SearchApiBaseAddress"] ?? "http://localhost:7072";
    client.BaseAddress = new Uri(searchApiBaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add additional services
builder.Services.AddScoped<PhoneticAnalyzersApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
