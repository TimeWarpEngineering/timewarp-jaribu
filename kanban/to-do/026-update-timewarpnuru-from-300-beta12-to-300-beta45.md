# Update TimeWarp.Nuru from 3.0.0-beta.12 to 3.0.0-beta.45

## Description

Update the TimeWarp.Nuru NuGet package dependency from version 3.0.0-beta.12 to 3.0.0-beta.45 across all projects in the solution.

## Checklist

- [ ] Identify all projects referencing TimeWarp.Nuru
- [ ] Identify all projects referencing TimeWarp.Nuru.Parsing
- [ ] Identify all projects referencing TimeWarp.Nuru.Analyzers
- [ ] Update version in Directory.Packages.props
- [ ] Run `dotnet restore` to verify new versions are resolved
- [ ] Build the solution to ensure compatibility
- [ ] Run tests to verify no regressions

## Notes

This is a significant version update (beta.12 → beta.45). Review the TimeWarp.Nuru changelog for breaking changes. TimeWarp.Nuru uses:
- Endpoints pattern (class-based with `[NuruRoute]` attribute)
- Fluent DSL pattern (inline with `Map()` calls)
- Source generator for endpoint discovery

**Version 3.0.0-beta.45 Info:**
- Commit: daf76e251d6aac57495dc58ed043cb565dc124b4
- Date: 2026-02-04

## TimeWarp.Nuru Reference

**Package:** `TimeWarp.Nuru`  
**Repository:** https://github.com/TimeWarpEngineering/timewarp-nuru

### Core Patterns

**Endpoints (class-based):**
```csharp
[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter] public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Hello, {query.Name}!");
      return default;
    }
  }
}
```

**Fluent DSL:**
```csharp
NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Map("version")
    .WithHandler(() => Console.WriteLine("1.0.0"))
    .AsQuery()
    .Done()
  .Build();
```

### Key Attributes

- `[NuruRoute("pattern", Description = "...")]` - Register route
- `[NuruRouteAlias("alias1", "alias2")]` - Multiple patterns
- `[NuruRouteGroup("prefix")]` - Shared prefix for derived commands
- `[Parameter]` - Positional parameter
- `[Option("name", "n")]` - Named option

### Handler Interfaces

- `ICommand<TResult>` - Actions with side effects
- `IQuery<TResult>` - Data retrieval (safe to retry)

### Common Pitfalls

1. Must call `DiscoverEndpoints()` for source generator to find routes
2. Route patterns use literals only; parameters from properties
3. Handler classes must be nested inside the route class
4. Constructor injection supported in handlers
