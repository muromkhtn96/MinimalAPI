# MinimalAPI — .NET 10, Minimal API, PostgreSQL, EF Core, MediatR, FluentValidation, Serilog

## Architecture: 4-layer DDD (Domain -> Application -> Infrastructure -> Api)
- Domain: Entities, VOs, Typed IDs, Events, Repo interfaces. No external deps.
- Application: CQRS/MediatR. Vertical slices: `Features/{Entity}/{UseCase}/`
- Infrastructure: EF Core, Repos, UoW
- Api: Endpoints, middleware, DI

## Naming
- `{Action}{Entity}Command/Query/Handler/Validator.cs`, `{Entity}Endpoints.cs`
- DB: tables lowercase plural, columns snake_case

## Rules
- Result<T> for commands, NO exceptions for business logic
- Factory methods + private setters on entities, sealed record VOs with Create()
- Typed IDs: `readonly record struct {Entity}Id(Guid Value)`
- Repos only for Aggregate Roots, Queries use IApplicationDbContext
- Handlers: primary constructors, no business logic — delegate to domain
- Endpoints: TypedResults, ISender, route group + WithTags()
- EF: ComplexProperty for VOs, HasConversion for Typed IDs
- Vietnamese user messages, English code
