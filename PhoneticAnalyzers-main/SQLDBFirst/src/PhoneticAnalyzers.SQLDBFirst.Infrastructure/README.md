# PhoneticAnalyzers.SQLDBFirst.Infrastructure

## Overview

This is the **Infrastructure layer** for the Database-First implementation. It contains:

- **Persistence**: EF Core DbContext and database configuration
- **Repositories**: Concrete implementations of repository interfaces
- **Services**: Concrete implementations of domain services
- **Dependency Injection**: Service registration extensions

## Structure

```
Infrastructure/
├── Persistence/
│   └── PhoneticDbContext.cs              # EF Core DbContext (will use scaffolded models)
├── Repositories/
│   ├── PersonRepository.cs               # CRUD operations for Person
│   ├── PersonSearchRepository.cs         # Advanced search with pg_trgm
│   └── NicknameMapRepository.cs          # Nickname bidirectional lookup
├── Services/
│   ├── PhoneticEncodingService.cs        # Double Metaphone & Beider-Morse
│   └── NicknameExpansionService.cs       # Nickname variant expansion
└── DependencyInjection.cs                # Service registration
```

## Key Features

### Database-First with EF Core

After scaffolding, the DbContext will use auto-generated models:

```powershell
# Generate models from database
cd SQLDBFirst
./scaffold-models.ps1

# This creates:
# - Models/Person.cs (with data annotations)
# - Models/PersonName.cs
# - Models/PhoneticDbContext.cs (scaffolded context)
```

**Option 1: Use Scaffolded Context Directly**
```csharp
// Replace PhoneticDbContext.cs with scaffolded version
// Update namespace and add any custom methods
```

**Option 2: Inherit from Scaffolded Context**
```csharp
public partial class PhoneticDbContext : ScaffoldedPhoneticDbContext
{
    // Add custom methods here
}
```

### PostgreSQL pg_trgm Integration

PersonSearchRepository uses raw SQL for trigram similarity:

```csharp
public async Task<IEnumerable<Person>> SearchByTrigramAsync(
    string searchName, double minSimilarity = 0.3)
{
    var sql = @"
        SELECT p.*
        FROM person p
        WHERE similarity(p.normalized_name, {0}) >= {1}
        ORDER BY similarity(p.normalized_name, {0}) DESC
        LIMIT 100";

    return await _context.Persons
        .FromSqlRaw(sql, normalized, minSimilarity)
        .ToListAsync();
}
```

### Phonetic Encoding with SharpNL

PhoneticEncodingService wraps SharpNL library:

```csharp
// Double Metaphone
var (primary, alternate) = _phoneticService.GetDoubleMetaphone("William");
// primary: "WLMN", alternate: null

// Beider-Morse (returns multiple codes)
var bmCodes = _phoneticService.GetBeiderMorseCodes("Schmidt");
// Returns: ["Shmit", "Smit", "Zmit", ...]
```

### Nickname Bidirectional Lookup

NicknameMapRepository handles complex nickname relationships:

```csharp
// Forward lookup: William -> [Bill, Billy, Will, Willy, Liam]
var nicknames = await _nicknameRepository.GetNicknamesAsync("WILLIAM");

// Reverse lookup: Bill -> [William, Billy, Will, Willy, Liam]
// (gets canonical name, then all its nicknames)
var billVariants = await _nicknameRepository.GetNicknamesAsync("BILL");
```

## Dependency Injection Setup

### In Azure Functions

```csharp
// Program.cs
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PhoneticDb");
        
        // Register Infrastructure
        services.AddSQLDBFirstInfrastructure(connectionString);
        
        // Register Application (MediatR handlers)
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(IngestPersonCommand).Assembly));
        
        // Register validators
        services.AddValidatorsFromAssembly(typeof(IngestPersonCommandValidator).Assembly);
    })
    .Build();
```

### Connection String Configuration

**local.settings.json:**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__PhoneticDb": "Host=localhost;Database=phonetic_db_dbfirst;Username=postgres;Password=your_password"
  }
}
```

**appsettings.json (for WebUI/DataSeeder):**
```json
{
  "ConnectionStrings": {
    "PhoneticDb": "Host=localhost;Database=phonetic_db_dbfirst;Username=postgres;Password=your_password"
  }
}
```

## Database-First Workflow Impact

When DBA updates schema:

1. **DBA creates SQL script** (e.g., `006_AddEmailColumn.sql`)
   ```sql
   ALTER TABLE person ADD COLUMN email VARCHAR(100);
   ```

2. **DBA runs script** on `phonetic_db_dbfirst`

3. **Developer re-scaffolds models**
   ```powershell
   cd SQLDBFirst
   ./scaffold-models.ps1
   ```

4. **Models auto-update** with new `Email` property

5. **Infrastructure uses updated models** automatically (no code changes needed)

6. **Application layer** may need updates if using new field

## Repository Pattern Benefits

**Testability:**
```csharp
// Mock repositories in unit tests
var mockRepo = new Mock<IPersonRepository>();
mockRepo.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new Person { PersonId = 1, FullName = "John Doe" });
```

**Flexibility:**
```csharp
// Easy to switch implementations
// - SQL Server: Change Npgsql to SqlServer provider
// - Dapper: Replace EF Core with Dapper
// - Caching: Add caching decorator
```

## Performance Considerations

### Indexes Used

PersonSearchRepository leverages database indexes:
- **GIN trigram index** on `normalized_name` for fuzzy search
- **B-tree index** on `primary_metaphone`, `alternate_metaphone`
- **GIN index** on `beider_morse`

### Query Optimization

```csharp
// Efficient: Uses index
var persons = await _context.Persons
    .Where(p => p.PrimaryMetaphone == "WLMN")
    .ToListAsync();

// Efficient: Uses pg_trgm GIN index
var fuzzyMatches = await SearchByTrigramAsync("William", 0.5);

// Include related data to avoid N+1 queries
var person = await _context.Persons
    .Include(p => p.PersonNames)
    .Include(p => p.PersonBms)
    .FirstOrDefaultAsync(p => p.PersonId == id);
```

## Dependencies

- **Microsoft.EntityFrameworkCore** (8.0.0): ORM framework
- **Npgsql.EntityFrameworkCore.PostgreSQL** (8.0.0): PostgreSQL provider
- **SharpNL.Extensions.PhoneticMatching** (1.2.0): Phonetic algorithms
- **Commons.Collections** (4.0.1): Helper collections

## Next Steps

1. Run SQL scripts to create `phonetic_db_dbfirst` database
2. Execute `scaffold-models.ps1` to generate entity models
3. Review scaffolded models in `SQLDBFirst/Models/`
4. Update PhoneticDbContext to use scaffolded models (or use scaffolded context directly)
5. Create Azure Functions that use this Infrastructure layer
6. Test repository implementations with real database

## Testing

```csharp
// Integration test example
public class PersonRepositoryTests : IDisposable
{
    private readonly PhoneticDbContext _context;
    private readonly PersonRepository _repository;

    public PersonRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PhoneticDbContext>()
            .UseNpgsql("Host=localhost;Database=phonetic_db_dbfirst_test;...")
            .Options;
        
        _context = new PhoneticDbContext(options);
        _repository = new PersonRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldCreatePerson()
    {
        var person = new Person 
        { 
            ExternalId = "TEST-001", 
            FullName = "John Doe" 
        };
        
        var id = await _repository.AddAsync(person);
        
        Assert.True(id > 0);
    }
}
```

## See Also

- [Domain Layer](../PhoneticAnalyzers.SQLDBFirst.Domain/README.md)
- [Application Layer](../PhoneticAnalyzers.SQLDBFirst.Application/README.md)
- [SQLDBFirst/README.md](../../README.md)
- [DatabaseScripts/](../../DatabaseScripts/)
