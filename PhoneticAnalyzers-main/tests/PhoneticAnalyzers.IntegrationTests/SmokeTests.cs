using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// removed duplicate using
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Application.Commands.Ingestion;
using PhoneticAnalyzers.Application.Queries.Search;
using PhoneticAnalyzers.Application.Services.Phonetic;
using PhoneticAnalyzers.Domain.Repositories;
using PhoneticAnalyzers.Infrastructure.Persistence;
using PhoneticAnalyzers.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PhoneticAnalyzers.IntegrationTests;

public class SmokeTests : IAsyncLifetime
{
    private ServiceProvider _provider = default!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // EF Core InMemory DB
        services.AddDbContext<PhoneticAnalyzersDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}");
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Repositories
        services.AddScoped<IPersonRepository, PersonRepository>();

        // Phonetic encoding services (same registrations as function host)
        services.AddSingleton<DoubleMetaphoneEncoder>();
        services.AddSingleton<BeiderMorseEncoder>();
        services.AddSingleton<IPhoneticEncoderFactory, PhoneticEncoderFactory>();
        services.AddScoped<IPhoneticEncodingService, PhoneticEncodingService>();
        services.AddSingleton<INicknameService, InMemoryNicknameService>();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IPhoneticEncodingService).Assembly));

        _provider = services.BuildServiceProvider();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_provider is IDisposable d)
        {
            d.Dispose();
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IngestPerson_Succeeds()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();

        var cmd = new IngestPersonCommand
        {
            ExternalId = "ext-1",
            FullName = "John Smith",
            ExpandNicknames = true
        };

        var result = await mediator.Send(cmd, CancellationToken.None);

    Assert.NotNull(result);
    Assert.True(result.WasCreated);
    Assert.True(result.PersonId > 0);

    var count = await repo.GetCountAsync();
    Assert.Equal(1, count);
    }

    [Fact]
    public async Task SearchAfterIngest_FindsExactMatch()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Arrange: ingest a person
        var ingest = new IngestPersonCommand
        {
            ExternalId = "ext-2",
            FullName = "Robert Martin",
            ExpandNicknames = true
        };
        var ingestResult = await mediator.Send(ingest, CancellationToken.None);
    Assert.True(ingestResult.WasCreated);

        // Act: search by the exact name
        var query = new SearchPersonsQuery
        {
            QueryName = "Robert Martin",
            MaxResults = 5,
            MinSimilarityThreshold = 0.1,
            IncludeTrigramSimilarity = false, // avoid provider-specific functions
            IncludeMatchDetails = false
        };

        var searchResult = await mediator.Send(query, CancellationToken.None);

        // Assert
    Assert.NotNull(searchResult);
    Assert.NotNull(searchResult.Matches);
    Assert.True(searchResult.Matches.Count > 0);
    Assert.Equal("Robert Martin", searchResult.Matches.First().FullName);
    }
}
