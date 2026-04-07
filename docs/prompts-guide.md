# MinimalAPI — Prompt Guide (.NET 10 + Angular)

**Mục tiêu**: Xây dựng hệ thống Product Management theo VSA + DDD 4 layers
**Stack**: .NET 10 Minimal API · PostgreSQL · EF Core · MediatR · Angular · Docker
**Cách dùng**: Copy từng prompt theo thứ tự, mỗi prompt hoàn thành rồi mới sang prompt tiếp

---

## PHASE 0: MÔI TRƯỜNG

```
Cài đặt môi trường tại D:\Projects\MinimalAPI:

1. Angular CLI: npm uninstall -g @angular/cli && npm install -g @angular/cli@latest

2. Tạo docker-compose.dev.yml:
   - PostgreSQL 17: port 5432, POSTGRES_DB=minimalapi_dev, POSTGRES_USER=postgres, POSTGRES_PASSWORD=postgres123, volume pgdata, healthcheck pg_isready
   - Seq: datalust/seq:latest, ports 5341+8081, env ACCEPT_EULA=Y + SEQ_FIRSTRUN_NOAUTHENTICATION=true, volume seqdata

3. docker compose -f docker-compose.dev.yml up -d
4. Verify: docker exec MinimalAPI.PostgresDB psql -U postgres -d minimalapi_dev -c "SELECT 1"
```

---

## PHASE 1: KHỞI TẠO

```
Tạo backend + frontend tại D:\Projects\MinimalAPI:

1. Backend Solution (.NET 10, DDD 4 layers):
   - dotnet new sln -n MinimalAPI -o backend
   - Tạo classlib: Domain, Application, Infrastructure
   - Tạo web: Api
   - Add vào sln, setup references: Api→(App,Infra), App→Domain, Infra→Domain
   - Xóa Class1.cs, verify: dotnet build

2. Frontend (Angular):
   - ng new minimal-app --directory frontend --routing --style=scss --ssr=false
   - Verify: ng serve

3. CLAUDE.md (tại root):
```markdown
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
```
```

---

## PHASE 2: DOMAIN

```
Tạo Domain layer trong MinimalAPI.Domain/:

1. Primitives/:
   - IDomainEvent: interface với DateTime OccurredAt
   - IHasDomainEvents: interface với IReadOnlyList<IDomainEvent> DomainEvents + void ClearDomainEvents() (dùng để scan generic từ ChangeTracker)
   - Entity<TId>: abstract, TId Id {protected init}, Equals/GetHashCode theo Id, protected ctor()
   - AggregateRoot<TId>: kế thừa Entity, implement IHasDomainEvents, List<IDomainEvent> + RaiseDomainEvent() + ClearDomainEvents()
   - Exceptions/DomainException.cs

2. Entities/ (Typed IDs):
   - ProductId: readonly record struct(Guid Value), static New()
   - CategoryId: readonly record struct(Guid Value), static New()

3. ValueObjects/:
   - Money: sealed record(decimal Amount, string Currency), private ctor, static Create() + VND() + Zero, validate >=0 + 3chars, operators +/*
   - ProductName: sealed record(string Value), private ctor, static Create(), validate NotEmpty/Max200/Trim

4. Entities/ (Aggregates):
   - Category: AggregateRoot<CategoryId>, props Name/Description?/CreatedAt, private setters + ctor, static Create(), method Update()
   - Product: AggregateRoot<ProductId>, props Name(ProductName)/Price(Money)/CategoryId/Description?/IsActive/CreatedAt/UpdatedAt?, private setters + ctor, static Create(), methods UpdateInfo()/UpdatePrice()/Activate()/Deactivate(), raise ProductCreatedEvent + ProductPriceChangedEvent

5. Events/:
   - ProductCreatedEvent(ProductId, DateTime OccurredAt)
   - ProductPriceChangedEvent(ProductId, Money OldPrice, Money NewPrice, DateTime OccurredAt)

6. Interfaces/:
   - IProductRepository: GetByIdAsync(ProductId), Add(Product), Remove(Product)
   - ICategoryRepository: GetByIdAsync(CategoryId), ExistsByNameAsync(string), Add(Category), GetAllAsync()
   - IUnitOfWork: SaveChangesAsync(CancellationToken)
```

---

## PHASE 3: INFRASTRUCTURE

```
Setup Infrastructure layer:

1. NuGet packages:
   - Infrastructure: Microsoft.EntityFrameworkCore + Npgsql.EntityFrameworkCore.PostgreSQL + Design
   - Application: MediatR + FluentValidation + FluentValidation.DependencyInjection
   - Api: Serilog.AspNetCore + Sinks (Console, File, Seq) + Enrichers (Environment, Process) + Swashbuckle.AspNetCore + EFCore.Design
   - Domain: KHÔNG cài gì

2. Infrastructure/Persistence/:
   - IApplicationDbContext: interface với IQueryable<Product> Products, IQueryable<Category> Categories (cho query side AsNoTracking)
   - DomainEventNotification: sealed record(IDomainEvent DomainEvent) : INotification (wrapper bridge Domain→MediatR)
   - AppDbContext: primary ctor(DbContextOptions, IMediator), DbSet<Product> + DbSet<Category>, implement IApplicationDbContext (return AsNoTracking), SaveChangesAsync (commit → scan IHasDomainEvents → collect events → clear → wrap DomainEventNotification → publish), OnModelCreating ApplyConfigurationsFromAssembly
   - Configurations/ProductConfiguration: table "products", HasConversion ProductId/CategoryId→Guid, ComplexProperty ProductName→"name"(max200), ComplexProperty Money→"price_amount"(decimal18,2)+"price_currency"(varchar3), Ignore DomainEvents
   - Configurations/CategoryConfiguration: table "categories", HasConversion CategoryId→Guid, Name required max100 unique index, Ignore DomainEvents

3. Repositories/:
   - ProductRepository: primary ctor(AppDbContext), implement IProductRepository
   - CategoryRepository: implement ICategoryRepository
   - UnitOfWork: implement IUnitOfWork, delegate AppDbContext.SaveChanges

4. DependencyInjection.cs:
   - AddInfrastructure(IServiceCollection, IConfiguration): DbContext (UseNpgsql "DefaultConnection" + EnableRetryOnFailure maxRetryCount=3 maxRetryDelay=5s), scoped IApplicationDbContext→AppDbContext, scoped IProductRepository/ICategoryRepository/IUnitOfWork

5. Api/appsettings:
   - appsettings.json: Serilog MinimumLevel Info
   - appsettings.Development.json: ConnectionStrings:DefaultConnection="Host=localhost;Port=5432;Database=minimalapi_dev;Username=postgres;Password=postgres123", Serilog WriteTo Console+File(logs/log-.txt daily) Override Microsoft.AspNetCore=Warning, EnableSwagger=true
   - appsettings.Production.json: ConnectionStrings:DefaultConnection="Host=db;Port=5432;Database=minimalapi_prod;Username=postgres;Password=postgres123", Serilog WriteTo File MinimumLevel Warning, EnableSwagger=false
   - launchSettings.json: profile "dev" ASPNETCORE_ENVIRONMENT=Development port 5000

6. Migration:
   - dotnet ef migrations add InitialCreate --project src/MinimalAPI.Infrastructure --startup-project src/MinimalAPI.Api
   - dotnet ef database update --startup-project src/MinimalAPI.Api
   - Verify: docker exec MinimalAPI.PostgresDB psql -U postgres -d minimalapi_dev -c "\dt"
```

---

## PHASE 4: APPLICATION

```
Tạo Application layer theo VSA:

1. Abstractions/:
   - Result<T>: record(T? Value, string? Error, bool IsSuccess), static Success(T)/Failure(string)
   - PagedResult<T>: record(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages, bool HasNext, bool HasPrevious)

2. Behaviors/:
   - ValidationBehavior: IPipelineBehavior, primary ctor(IEnumerable<IValidator<TRequest>>), validate → throw ValidationException nếu lỗi
   - LoggingBehavior: IPipelineBehavior, log request name + Stopwatch elapsed

3. DependencyInjection.cs:
   - AddApplication(IServiceCollection): MediatR from assembly, AddOpenBehavior ValidationBehavior + LoggingBehavior, AddValidatorsFromAssembly

4. Features/Products/:
   - DTOs/ProductDto: record(Guid Id, string Name, decimal Price, string Currency, Guid CategoryId, string CategoryName, string? Description, bool IsActive, DateTime CreatedAt)
   - CreateProduct/: Command(Name, Price, Currency, CategoryId, Description?) : IRequest<Result<Guid>>, Validator (Name NotEmpty Max200, Price>0, Currency NotEmpty Len3, CategoryId NotEmpty, messages Vietnamese), Handler (primary ctor inject ICategoryRepository/IProductRepository/IUnitOfWork, check category exists → ProductName.Create + Money.Create → Product.Create → repo.Add → uow.Save → Success(id))
   - GetProduct/: Query(Guid Id) : IRequest<ProductDto?>, Handler (primary ctor inject IApplicationDbContext, LINQ Join Products+Categories AsNoTracking → Select new ProductDto)
   - GetProducts/: Query(int Page=1, int PageSize=10, string? Search=null) : IRequest<PagedResult<ProductDto>>, Handler (LINQ Join + Where search Name.Value.Contains + CountAsync + OrderByDescending CreatedAt + Skip/Take AsNoTracking → Select new ProductDto)
   - UpdateProduct/: Command(Guid Id, Name, Price, Currency, Description?) : IRequest<Result>, Validator, Handler (repo.GetByIdAsync → UpdateInfo + UpdatePrice → uow.Save)
   - DeleteProduct/: Command(Guid Id) : IRequest<Result>, Handler (repo.GetByIdAsync → repo.Remove → uow.Save)

5. Features/Categories/:
   - DTOs/CategoryDto: record(Guid Id, string Name, string? Description, DateTime CreatedAt)
   - CreateCategory/: Command(Name, Description?) : IRequest<Result<Guid>>, Validator (Name NotEmpty Max100, check ExistsByNameAsync), Handler (Category.Create → repo.Add → uow.Save)
   - GetCategories/: Query(int Page=1, int PageSize=20) : IRequest<PagedResult<CategoryDto>>, Handler (LINQ AsNoTracking)
   - GetCategory/: Query(Guid Id) : IRequest<CategoryDto?>, Handler (LINQ AsNoTracking FirstOrDefault)
   - UpdateCategory/: Command(Guid Id, Name, Description?) : IRequest<Result>, Validator, Handler (repo.GetByIdAsync → Update → uow.Save)
   - DeleteCategory/: Command(Guid Id) : IRequest<Result>, Handler (check products.Count via IApplicationDbContext → repo.GetByIdAsync → repo.Remove → uow.Save)
```

---

## PHASE 5: API

```
Tạo API layer:

1. Api/Program.cs:
   - UseSerilog(readFrom appsettings)
   - Services: AddApplication() + AddInfrastructure(config) + AddHealthChecks().AddDbContextCheck<AppDbContext>() + AddRateLimiter(FixedWindow 100req/1min per IP) + AddProblemDetails + AddEndpointsApiExplorer + AddSwaggerGen("MinimalAPI", "v1") + AddCors("frontend")
   - Pipeline: UseExceptionHandler (ValidationException→400 ProblemDetails, DomainException→400, else→500, include traceId) → UseHttpsRedirection if !Development → UseCors → UseRateLimiter → UseSwagger/UseSwaggerUI if Development → MapProductEndpoints + MapCategoryEndpoints + MapHealthChecks("/health")
   - CORS: Development AllowOrigins "http://localhost:4200", Production AllowOrigins from config "AllowedOrigins", both AllowAnyHeader/Method
   - Auto migrate + Seed: using scope → db.Database.MigrateAsync() → if Development SeedData.SeedAsync(db)
   - app.Run()

2. Api/Endpoints/ProductEndpoints.cs:
   - MapProductEndpoints(this WebApplication): var group = app.MapGroup("/api/products").WithTags("Products")
   - GET / (int page=1, int pageSize=10, string? search) → inject ISender → Send GetProductsQuery → TypedResults.Ok(pagedResult)
   - GET /{id:guid} → GetProductQuery → result ? Ok(result) : NotFound()
   - POST / (CreateProductCommand cmd) → Send → result.IsSuccess ? Created($"/api/products/{id}", id) : BadRequest(result.Error), WithName/Summary/Produces
   - PUT /{id:guid} (body) → UpdateProductCommand → NoContent/BadRequest/NotFound
   - DELETE /{id:guid} → DeleteProductCommand → NoContent/NotFound

3. Api/Endpoints/CategoryEndpoints.cs:
   - MapCategoryEndpoints(this WebApplication): var group = app.MapGroup("/api/categories").WithTags("Categories")
   - CRUD endpoints tương tự Products

4. Infrastructure/Persistence/SeedData.cs:
   - static async SeedAsync(AppDbContext): if context.Categories.Any() return → 3 categories via Category.Create() → AddRange + SaveChanges → 5 products via Product.Create(ProductName.Create, Money.VND, categoryId) → AddRange + SaveChanges

5. Test: dotnet run --project backend/src/MinimalAPI.Api, Swagger http://localhost:5000/swagger, test CRUD, verify logs console + file logs/, verify /health endpoint 200 OK
```

---

## PHASE 6: FRONTEND

```
Setup Angular frontend:

1. Bootstrap: npm install bootstrap, import vào styles.scss: @import "bootstrap/scss/bootstrap";

2. Cấu trúc src/app/:
   - core/models/: Product, Category, PagedResult<T> interfaces
   - core/services/: ProductService (getAll/getById/create/update/delete), CategoryService (getAll/create/update/delete), HttpClient Observable
   - core/interceptors/: ErrorInterceptor (catch 400/404/500)
   - features/home/: HomeComponent ("MinimalAPI — Hệ thống quản lý sản phẩm")
   - features/products/: ProductListComponent (table Name/Price/Category/IsActive, pagination, search, buttons Add/Edit/Delete confirm), ProductFormComponent (ReactiveForm shared Create/Edit detect by route id, Name required max200, Price required >0, Currency dropdown VND/USD, Category dropdown, Description, validation inline, submit navigate /products)
   - features/categories/: CategoryListComponent + CategoryFormComponent (simple Name/Description)
   - shared/components/: navbar (Home, Products, Categories)

3. Environment: environment.ts apiUrl='http://localhost:5000/api'

4. app.config.ts: provideHttpClient()

5. Routing: / → Home, /products → list, /products/create → form, /products/edit/:id → form, /categories → list, /categories/create → form, /categories/edit/:id → form, ** → 404

6. Loading state + disable submit during request

7. Test: ng serve, http://localhost:4200, test CRUD both Products/Categories
```

---

## PHASE 7: DOCKER & HOÀN THIỆN

```
Dockerize và hoàn thiện project:

1. backend/Dockerfile (multi-stage):
   - build: mcr.microsoft.com/dotnet/sdk:10.0-preview, COPY *.csproj restore, COPY . publish -c Release -o /app
   - runtime: mcr.microsoft.com/dotnet/aspnet:10.0-preview, WORKDIR /app, COPY --from=build, USER app, EXPOSE 8080, ENTRYPOINT dotnet MinimalAPI.Api.dll

2. frontend/Dockerfile (multi-stage):
   - build: node:20, WORKDIR /app, COPY package*.json npm install, COPY . ng build --configuration production
   - runtime: nginx:alpine, COPY --from=build /app/dist/minimal-app /usr/share/nginx/html, COPY nginx.conf /etc/nginx/conf.d/default.conf

3. frontend/nginx.conf:
   - server listen 80, root /usr/share/nginx/html, index index.html
   - location / try_files $uri $uri/ /index.html
   - location /api/ proxy_pass http://api:8080/api/

4. docker-compose.yml (Production full stack):
   - db: postgres:17, MinimalAPI.PostgresDB, POSTGRES_DB=minimalapi_prod, volume pgdata, healthcheck pg_isready
   - api: build ./backend, MinimalAPI.Host, ASPNETCORE_ENVIRONMENT=Production, ConnectionStrings__DefaultConnection override, depends_on db healthy, ports 5000:8080
   - web: build ./frontend, MinimalAPI.Angular, depends_on api, ports 80:80

5. Cập nhật docker-compose.dev.yml:
   - Thêm api: build ./backend, depends_on db+seq, ASPNETCORE_ENVIRONMENT=Development, ConnectionStrings__DefaultConnection, Serilog__WriteTo__2__Args__serverUrl=http://seq:5341, ports 5000:8080

6. .dockerignore: backend (bin/obj/.vs/logs/), frontend (node_modules/dist/.angular/)

7. .gitignore: bin/, obj/, node_modules/, dist/, logs/, .vs/, .idea/, *.user, docker-compose.override.yml

8. README.md:
   - MinimalAPI — Product Management System
   - Tech: .NET 10, Angular 19, PostgreSQL, Docker
   - Structure: backend (Domain/Application/Infrastructure/Api), frontend, docs
   - Development: docker compose -f docker-compose.dev.yml up -d, dotnet run --project backend/src/MinimalAPI.Api, cd frontend && ng serve
   - Production: docker compose up --build
   - Swagger: http://localhost:5000/swagger (Development only)

9. Test full flow:
   - docker compose up --build → http://localhost → test UI CRUD → verify db data → docker compose down
   - Verify: dotnet build (no warning), ng build (no error)
```

---

## TÓM TẮT

**7 Phases → 7 Prompts tối ưu**

| Phase | Nội dung | Kết quả |
|-------|----------|---------|
| 0 | Môi trường | Docker PostgreSQL + Seq |
| 1 | Khởi tạo | BE solution + FE Angular + CLAUDE.md |
| 2 | Domain | Primitives + Entities + VOs + Repos |
| 3 | Infrastructure | EF Core + DbContext + Migration |
| 4 | Application | MediatR + Behaviors + CRUD Features |
| 5 | API | Program.cs + Endpoints + Seed |
| 6 | Frontend | Angular Services + UI Components |
| 7 | Docker | Dockerfile + docker-compose + README |

**Conventions chính:**
- 4-layer DDD: Domain (pure) → Application (VSA) → Infrastructure (EF) → Api (Minimal)
- Typed IDs (readonly record struct), VOs (sealed record), Result<T> pattern
- Command (tracked) vs Query (AsNoTracking), primary constructors, TypedResults
- DB snake_case, Vietnamese user messages, English code
