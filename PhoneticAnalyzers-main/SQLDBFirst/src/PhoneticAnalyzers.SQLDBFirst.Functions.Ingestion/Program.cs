using FluentValidation;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhoneticAnalyzers.SQLDBFirst.Application.Commands;
using PhoneticAnalyzers.SQLDBFirst.Application.Validators;
using PhoneticAnalyzers.SQLDBFirst.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Get connection string from environment
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PhoneticDb")
            ?? throw new InvalidOperationException("ConnectionStrings__PhoneticDb not configured");

        // Register Infrastructure (DbContext, Repositories, Services)
        services.AddSQLDBFirstInfrastructure(connectionString);

        // Register MediatR (Application handlers)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(IngestPersonCommand).Assembly));

        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(IngestPersonCommandValidator).Assembly);
    })
    .Build();

host.Run();
