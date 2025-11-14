# PhoneticAnalyzers.SQLDBFirst.Domain

## Overview

This is the **Domain layer** for the Database-First implementation of PhoneticAnalyzers. It contains:

- **Entities**: Placeholder entity classes (Person, PersonName, PersonBm, NicknameMap, etc.)
- **Repository Interfaces**: Contracts for data access
- **Service Interfaces**: Contracts for business operations
- **Common Types**: Enums, value objects, result types

## Important Notes

### Scaffolded Models Will Replace Entities

The entity classes in `Entities/` are **temporary placeholders**. After running the scaffold script:

```powershell
cd SQLDBFirst
./scaffold-models.ps1
```

The generated models in `SQLDBFirst/Models/` should be used instead. You have two options:

**Option 1: Replace Entity Files (Recommended)**
- Delete files in `Entities/` folder
- Copy scaffolded models from `Models/` to `Entities/`
- Update namespaces to `PhoneticAnalyzers.SQLDBFirst.Domain.Entities`

**Option 2: Reference Models Directly**
- Keep placeholder entities for reference
- Update repository implementations to use `SQLDBFirst.Models` namespace
- Add project reference to Models

## Structure

```
Domain/
├── Entities/                    # Placeholder entities (will be replaced by scaffolded models)
│   ├── Person.cs               # Person entity
│   ├── PersonName.cs           # Name tokens for searching
│   ├── PersonBm.cs             # Beider-Morse phonetic codes
│   ├── NicknameMap.cs          # Nickname mappings
│   ├── NameAlias.cs            # Alternative spellings
│   └── NameAliasCache.cs       # Performance cache
├── Repositories/                # Repository interfaces
│   ├── IPersonRepository.cs    # Person CRUD operations
│   ├── IPersonSearchRepository.cs  # Advanced search operations
│   └── INicknameMapRepository.cs   # Nickname lookup operations
├── Services/                    # Service interfaces
│   ├── IPhoneticEncodingService.cs    # Phonetic encoding
│   └── INicknameExpansionService.cs   # Nickname expansion
└── Common/                      # Shared types
    ├── MatchType.cs            # Search match types enum
    ├── SearchResult.cs         # Search result with metadata
    └── OperationResult.cs      # Generic operation result
```

## Key Concepts

### Repository Pattern

Repositories abstract data access. They define contracts that Infrastructure layer implements:

```csharp
// Domain defines the contract
public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(long personId, CancellationToken cancellationToken);
    Task<long> AddAsync(Person person, CancellationToken cancellationToken);
}

// Infrastructure implements using EF Core and scaffolded DbContext
public class PersonRepository : IPersonRepository
{
    private readonly PhoneticDbContext _context;
    // Implementation using scaffolded models...
}
```

### Service Pattern

Services encapsulate business logic:

```csharp
// Domain defines the contract
public interface IPhoneticEncodingService
{
    (string? primary, string? alternate) GetDoubleMetaphone(string text);
}

// Infrastructure implements using NuGet packages
public class PhoneticEncodingService : IPhoneticEncodingService
{
    // Implementation using DoubleMetaphone library...
}
```

## Database-First Workflow

1. **DBA creates SQL script** (e.g., `005_AddEmailColumn.sql`)
2. **DBA runs script** on `phonetic_db_dbfirst` database
3. **Developer runs scaffold script** to regenerate models:
   ```powershell
   cd SQLDBFirst
   ./scaffold-models.ps1
   ```
4. **Developer updates Domain entities** (if needed) to match scaffolded models
5. **Commit changes** to version control

## Dependencies

- **MediatR**: For CQRS pattern (Commands and Queries)
- **No EF Core in Domain**: Domain is persistence-agnostic

## Next Steps

1. Run `SQLDBFirst/scaffold-models.ps1` to generate models from database
2. Review generated models in `SQLDBFirst/Models/`
3. Decide whether to replace Entity files or reference Models directly
4. Implement Application layer (handlers, commands, queries)
5. Implement Infrastructure layer (repositories, DbContext)

## See Also

- [SQLDBFirst/README.md](../../README.md) - Complete Database-First documentation
- [SQLDBFirst/QUICK-START.md](../../QUICK-START.md) - 5-minute setup guide
- [DatabaseScripts/](../../DatabaseScripts/) - SQL schema and seed scripts
