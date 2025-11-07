using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

// Simple console harness to smoke-test ingest + search without test runner dependencies

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddDbContext<PhoneticAnalyzersDbContext>(options =>
{
    options.UseInMemoryDatabase($"HarnessDb_{Guid.NewGuid():N}");
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
});
services.AddScoped<IPersonRepository, PersonRepository>();
services.AddSingleton<DoubleMetaphoneEncoder>();
services.AddSingleton<BeiderMorseEncoder>();
services.AddSingleton<IPhoneticEncoderFactory, PhoneticEncoderFactory>();
services.AddScoped<IPhoneticEncodingService, PhoneticEncodingService>();
services.AddSingleton<INicknameService, InMemoryNicknameService>();
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IPhoneticEncodingService).Assembly));

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();

// Ingest test
var ingest = new IngestPersonCommand { ExternalId = "ext-1", FullName = "John Smith", ExpandNicknames = true };
var ingestResult = await mediator.Send(ingest, CancellationToken.None);
Console.WriteLine($"Ingest: WasCreated={ingestResult.WasCreated}, PersonId={ingestResult.PersonId}");

// Search test
var query = new SearchPersonsQuery { QueryName = "John Smith", MaxResults = 5, IncludeTrigramSimilarity = false };
var searchResult = await mediator.Send(query, CancellationToken.None);
Console.WriteLine($"Search: Matches={searchResult.Matches.Count}, TopFullName={searchResult.Matches.FirstOrDefault()?.FullName}");

// Exit code 0 on success (at least one match)
return searchResult.Matches.Count > 0 ? 0 : 1;