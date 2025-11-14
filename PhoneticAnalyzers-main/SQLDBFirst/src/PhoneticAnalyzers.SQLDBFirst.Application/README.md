# PhoneticAnalyzers.SQLDBFirst.Application

## Overview

This is the **Application layer** for the Database-First implementation. It contains:

- **Commands**: Write operations (IngestPerson, BatchIngest)
- **Queries**: Read operations (SearchPersons, GetDatabaseStats)
- **Handlers**: MediatR handlers implementing CQRS pattern
- **DTOs**: Data Transfer Objects for API communication
- **Validators**: FluentValidation rules for input validation

## Structure

```
Application/
├── Commands/
│   ├── IngestPersonCommand.cs        # Single person ingestion
│   └── BatchIngestCommand.cs         # Bulk person ingestion
├── Queries/
│   ├── SearchPersonsQuery.cs         # Phonetic search query
│   └── GetDatabaseStatsQuery.cs      # Database statistics
├── Handlers/
│   ├── IngestPersonCommandHandler.cs        # Creates person with phonetic codes
│   ├── BatchIngestCommandHandler.cs         # Processes bulk ingestion
│   ├── SearchPersonsQueryHandler.cs         # Multi-algorithm search
│   └── GetDatabaseStatsQueryHandler.cs      # Returns DB stats
├── DTOs/
│   ├── PersonIngestDto.cs           # Input DTO for ingestion
│   └── PersonSearchResultDto.cs     # Output DTO for search
└── Validators/
    ├── IngestPersonCommandValidator.cs  # Validates ingestion input
    └── SearchPersonsQueryValidator.cs   # Validates search input
```

## Key Concepts

### CQRS Pattern

Commands and Queries are separated:

**Commands** (Write):
```csharp
public class IngestPersonCommand : IRequest<OperationResult<long>>
{
    public string ExternalId { get; set; }
    public string FullName { get; set; }
    public bool ExpandNicknames { get; set; }
}
```

**Queries** (Read):
```csharp
public class SearchPersonsQuery : IRequest<List<PersonSearchResultDto>>
{
    public string SearchName { get; set; }
    public double MinSimilarity { get; set; }
    public bool ExpandNicknames { get; set; }
}
```

### MediatR Handlers

Handlers implement business logic:

```csharp
public class IngestPersonCommandHandler 
    : IRequestHandler<IngestPersonCommand, OperationResult<long>>
{
    public async Task<OperationResult<long>> Handle(
        IngestPersonCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Validate
        // 2. Normalize name
        // 3. Generate phonetic codes
        // 4. Create entity
        // 5. Save to database
        // 6. Generate nickname variants (optional)
        // 7. Return result
    }
}
```

### Nickname Variant Generation

When `ExpandNicknames = true`, the system automatically creates variant persons:

**Example:**
```
Input: William Smith (ID: 12345)
Generated Variants:
- Bill Smith (12345-NICK-BILL)
- Billy Smith (12345-NICK-BILLY)
- Will Smith (12345-NICK-WILL)
- Willy Smith (12345-NICK-WILLY)
- Liam Smith (12345-NICK-LIAM)
```

### Search Algorithms

SearchPersonsQueryHandler uses multiple algorithms:

1. **Exact Match** (1.0 confidence)
2. **Token Contains** (0.95 confidence)
3. **Nickname Expansion** (0.93 confidence) - if enabled
4. **Primary Metaphone** (0.9 confidence)
5. **Alternate Metaphone** (0.85 confidence)
6. **Beider-Morse** (0.8 confidence)
7. **Trigram Similarity** (variable based on `similarity()` score)

Results are deduplicated by `PersonId` and sorted by `MatchScore` descending.

## Validation

FluentValidation ensures data quality:

```csharp
public class IngestPersonCommandValidator : AbstractValidator<IngestPersonCommand>
{
    public IngestPersonCommandValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200)
            .Must(ContainAtLeastTwoNames);
    }
}
```

## Database-First Independence

This Application layer is **database-agnostic**. It depends only on:
- Domain interfaces (repositories, services)
- Domain entities

The Infrastructure layer provides concrete implementations using scaffolded models.

## Dependencies

- **MediatR** (12.2.0): CQRS pattern, handler pipeline
- **FluentValidation** (11.9.0): Input validation
- **PhoneticAnalyzers.SQLDBFirst.Domain**: Entities, interfaces

## Usage in Azure Functions

```csharp
// Ingestion Function
[Function("IngestPerson")]
public async Task<IActionResult> IngestPerson(
    [HttpTrigger(AuthorizationLevel.Function, "post")] 
    HttpRequest req)
{
    var dto = await req.ReadFromJsonAsync<PersonIngestDto>();
    
    var command = new IngestPersonCommand
    {
        ExternalId = dto.ExternalId,
        FullName = dto.FullName,
        County = dto.County,
        ExpandNicknames = dto.ExpandNicknames
    };

    var result = await _mediator.Send(command);
    
    return result.IsSuccess 
        ? new OkObjectResult(result) 
        : new BadRequestObjectResult(result);
}

// Search Function
[Function("SearchPersons")]
public async Task<IActionResult> SearchPersons(
    [HttpTrigger(AuthorizationLevel.Function, "get")] 
    HttpRequest req)
{
    var query = new SearchPersonsQuery
    {
        SearchName = req.Query["name"],
        MinSimilarity = double.Parse(req.Query["minSimilarity"] ?? "0.3"),
        ExpandNicknames = bool.Parse(req.Query["expandNicknames"] ?? "false")
    };

    var results = await _mediator.Send(query);
    
    return new OkObjectResult(results);
}
```

## Next Steps

1. Create Infrastructure layer with repository implementations
2. Create Azure Functions (Ingestion on 7073, Search on 7074)
3. Register handlers in DI container
4. Test with scaffolded models from database

## See Also

- [Domain Layer](../PhoneticAnalyzers.SQLDBFirst.Domain/README.md)
- [SQLDBFirst/README.md](../../README.md)
- [DatabaseScripts/](../../DatabaseScripts/)
