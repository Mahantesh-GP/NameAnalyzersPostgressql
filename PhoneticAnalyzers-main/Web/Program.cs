using PhoneticAnalyzers.Web.Components;
using PhoneticAnalyzers.Web.Services;
using PhoneticAnalyzers.Infrastructure.Persistence;
using PhoneticAnalyzers.Infrastructure.Persistence.Repositories;
using PhoneticAnalyzers.Application.Commands.Ingestion;
using PhoneticAnalyzers.Application.Services.Phonetic;
using PhoneticAnalyzers.Application.Services.Text;
using PhoneticAnalyzers.Application.Services.Nicknames;
using PhoneticAnalyzers.Application.Services.LLM;
using PhoneticAnalyzers.Application.Behaviors;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Domain.Services;
using Microsoft.EntityFrameworkCore;
using MediatR;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Enable detailed circuit errors during development to diagnose issues
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=PhoneticAnalyzersDb;Username=postgres;Password=postgres;Port=5432;";

builder.Services.AddDbContext<PhoneticAnalyzersDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(PhoneticAnalyzersDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Repositories
builder.Services.AddScoped<IPersonRepository, PersonRepository>();

// Multilingual Search Repositories
builder.Services.AddScoped<IPersonNameRepository, PersonNameRepository>();
builder.Services.AddScoped<INameAliasRepository, NameAliasRepository>();
builder.Services.AddScoped<INicknameMapRepository, NicknameMapRepository>();
builder.Services.AddScoped<INameAliasCacheRepository, NameAliasCacheRepository>();

// Text processing services
builder.Services.AddScoped<ITextNormalizationService, TextNormalizationService>();

// Curated nickname service
builder.Services.AddScoped<ICuratedNicknameService, SimpleCuratedNicknameService>();

// Phonetic encoding services
builder.Services.AddSingleton<DoubleMetaphoneEncoder>();
builder.Services.AddSingleton<BeiderMorseEncoder>();
builder.Services.AddSingleton<IPhoneticEncoderFactory, PhoneticEncoderFactory>();
builder.Services.AddScoped<IPhoneticEncodingService, PhoneticEncodingService>();
builder.Services.AddSingleton<INicknameService, InMemoryNicknameService>();

// MediatR for CQRS
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncodingService).Assembly));

// FluentValidation: register validators & pipeline behavior
builder.Services.AddValidatorsFromAssembly(typeof(PhoneticAnalyzers.Application.Services.Phonetic.IPhoneticEncodingService).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

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
