# Prompt Guide — MinimalAPI (.NET 10 + Angular)

> **Cách dùng**: Copy từng prompt, gửi cho Claude Code CLI theo thứ tự.
> Mỗi prompt hoàn thành rồi mới chuyển sang prompt tiếp theo.
>
> **Project**: MinimalAPI — Quản lý sản phẩm (Product Management)
> **Stack**: .NET 10 Minimal API (VSA + DDD) · Angular · PostgreSQL · Serilog · Swagger
> **Environments**: Development + Production

---

## PHASE 0: CÀI ĐẶT MÔI TRƯỜNG

### Prompt 0.1 — Cài đặt môi trường

```
Cài đặt môi trường phát triển cho project MinimalAPI trên Windows:

1. Angular CLI: gỡ bản cũ (v13), cài latest stable:
   npm uninstall -g @angular/cli && npm install -g @angular/cli@latest
   Verify: ng version

2. PostgreSQL + Seq qua Docker — tạo file docker-compose.dev.yml tại D:\Projects\MinimalAPI:
   - PostgreSQL 17, port 5432
   - container_name: MinimalAPI.PostgresDB
   - POSTGRES_DB: minimalapi_dev
   - POSTGRES_USER: postgres
   - POSTGRES_PASSWORD: postgres123
   - Volume: pgdata
   - Healthcheck: pg_isready
   - Seq (datalust/seq:latest): port 5341 (ingestion), port 8081 (UI)
   - container_name: MinimalAPI.Seq
   - Volume: seqdata

3. Chạy: docker compose -f docker-compose.dev.yml up -d
4. Verify kết nối: docker exec vào container, psql -U postgres -d minimalapi_dev -c "SELECT 1"
5. Verify Seq UI: http://localhost:8081

KHÔNG cài Redis, KHÔNG cài Kafka.
```

---

## PHASE 1: KHỞI TẠO PROJECT

### Prompt 1.1 — Solution Backend (DDD 4 layers)

```
Tạo Solution .NET 10 theo DDD tại D:\Projects\MinimalAPI\backend, tên solution MinimalAPI:

backend/
├── src/
│   ├── MinimalAPI.Domain/           # Pure C#, không dependency
│   ├── MinimalAPI.Application/      # Phụ thuộc Domain
│   ├── MinimalAPI.Infrastructure/   # Phụ thuộc Domain
│   └── MinimalAPI.Api/              # Phụ thuộc Application + Infrastructure
└── MinimalAPI.sln

Bước thực hiện:
1. dotnet new sln -n MinimalAPI -o backend
2. Tạo projects:
   - Domain, Application, Infrastructure: dotnet new classlib
   - Api: dotnet new web
3. Add projects vào solution
4. Project references (dependency rule):
   - Application → Domain
   - Infrastructure → Domain
   - Api → Application + Infrastructure
   - Domain → KHÔNG reference gì
5. Xóa file mặc định (Class1.cs...)
6. Verify: dotnet build backend/MinimalAPI.sln
```

### Prompt 1.2 — Angular Frontend

```
Tạo Angular project tại D:\Projects\MinimalAPI\frontend:

1. Chạy: ng new minimal-app --directory frontend --routing --style=scss --ssr=false
2. Verify chạy được: ng serve (thử rồi tắt)
3. Confirm port mặc định 4200 trong angular.json

Chưa cần thêm thư viện nào.
```

### Prompt 1.3 — Claude CLI (tối ưu token)

```
Tạo cấu hình Claude Code CLI tại D:\Projects\MinimalAPI.
Mục tiêu: hỗ trợ + quản trị + code cho project dài hạn. Tối ưu token — giữ mọi thứ ngắn gọn.

1. Tạo CLAUDE.md tại root (file này load MỌI session — phải ngắn):

---
# MinimalAPI

## Stack
BE: .NET 10 Minimal API, VSA, DDD, PostgreSQL, Serilog, Swagger
FE: Angular, SCSS, Bootstrap

## Architecture
```
Api → Application → Domain ← Infrastructure
```
- Domain: pure C#, không dependency
- Application: MediatR + FluentValidation, features theo VSA
- Infrastructure: EF Core + Npgsql
- Api: Minimal API endpoints, Serilog, Swagger

## Conventions
- Primary constructor cho DI
- Result<T> pattern (không throw cho business logic)
- TypedResults cho endpoints
- Strongly Typed IDs (readonly record struct)
- Value Objects: sealed record + private ctor + static Create()
- DB: snake_case (bảng + cột)
- CQRS: Command = EF Core (tracked), Query = EF Core LINQ (AsNoTracking)

## Structure
- backend/src/MinimalAPI.{Domain,Application,Infrastructure,Api}/
- frontend/ (Angular)
- docker-compose.dev.yml (PostgreSQL dev)
- docker-compose.yml (full stack production)

## Environments
- Development: Swagger UI, detailed logs, seed data, CORS localhost:4200
- Production: no Swagger, minimal logs, no seed, CORS production domain
---

2. Tạo .claude/ — CHỈ những gì thực sự cần, không tạo thừa:

.claude/agents/implementer.md:
```
# Implementer
Thực thi code changes theo convention trong CLAUDE.md.
- Đọc CLAUDE.md trước khi code
- Giữ changes nhỏ, traceable
- Follow VSA: mỗi feature 1 folder
- Verify build pass sau khi thay đổi
```

.claude/agents/reviewer.md:
```
# Reviewer
Review code về correctness, convention, security.
- Check dependency rule (Domain không phụ thuộc gì)
- Check convention trong CLAUDE.md
- Trả findings theo severity: critical > warning > info
```

.claude/commands/implement.md:
```
Implement task theo CLAUDE.md conventions. Tóm tắt files changed và remaining gaps.
```

.claude/commands/review.md:
```
Review current changes. Check architecture rules, conventions, security. Required fixes trước, suggestions sau.
```

.claude/commands/diagnose.md:
```
Điều tra issue, xác định root cause, đề xuất fix.
```

KHÔNG tạo thêm agents/skills/commands khác. Giữ tối thiểu để tiết kiệm token.
3. Tạo .claude/settings.json nếu cần thiết lập permission mặc định.
```

---

## PHASE 2: BACKEND — DOMAIN LAYER

### Prompt 2.1 — Domain Primitives

```
Tạo base classes trong MinimalAPI.Domain/Primitives/:

1. IDomainEvent.cs — interface: DateTime OccurredAt
2. Entity<TId>.cs — abstract class, TId : notnull
   - TId Id { get; protected init; }
   - Override Equals/GetHashCode (so sánh bằng Id)
   - Protected parameterless ctor (EF Core)
3. AggregateRoot<TId>.cs — kế thừa Entity<TId>
   - Private List<IDomainEvent>, public IReadOnlyList
   - Protected RaiseDomainEvent(), public ClearDomainEvents()
4. Domain/Exceptions/DomainException.cs — kế thừa Exception

Comment tiếng Việt ngắn gọn cho intern.
```

### Prompt 2.2 — Strongly Typed IDs + Value Objects

```
Tạo trong MinimalAPI.Domain:

Entities/ (Strongly Typed IDs):
1. ProductId — readonly record struct(Guid Value), static New()
2. CategoryId — readonly record struct(Guid Value), static New()

ValueObjects/:
1. Money.cs — sealed record, Amount + Currency
   - Private ctor + static Create(decimal, string)
   - Validate: amount >= 0, currency 3 chars
   - Static: VND(decimal), Zero
   - Operators: + (cùng currency), * (quantity)

2. ProductName.cs — sealed record, Value (string)
   - Private ctor + static Create(string)
   - Validate: not empty, max 200, trim

Comment giải thích Value Object vs Entity cho intern.
```

### Prompt 2.3 — Entities & Domain Events

```
Tạo Entities trong MinimalAPI.Domain/Entities/:

1. Category.cs — AggregateRoot<CategoryId>:
   - Name (string), Description (string?), CreatedAt (DateTime)
   - Private setters, private parameterless ctor
   - Static Create(string name, string? description)
   - Method: Update(string name, string? description)

2. Product.cs — AggregateRoot<ProductId>:
   - Name (ProductName), Price (Money), CategoryId, Description (string?),
     IsActive (bool), CreatedAt, UpdatedAt (DateTime?)
   - Private setters, private parameterless ctor
   - Static Create(ProductName, Money, CategoryId, string?)
   - Methods: UpdateInfo(), UpdatePrice(), Deactivate(), Activate()
   - Raise ProductCreatedEvent khi tạo, ProductPriceChangedEvent khi đổi giá

Domain/Events/:
- ProductCreatedEvent(ProductId) : IDomainEvent
- ProductPriceChangedEvent(ProductId, Money OldPrice, Money NewPrice) : IDomainEvent

KHÔNG public setter. Mọi thay đổi qua method.
```

### Prompt 2.4 — Repository Interfaces

```
Tạo interfaces trong MinimalAPI.Domain/Interfaces/:

1. IProductRepository — GetByIdAsync(ProductId), Add(Product), Remove(Product)
2. ICategoryRepository — GetByIdAsync(CategoryId), ExistsByNameAsync(string), Add(Category)
3. IUnitOfWork — SaveChangesAsync(CancellationToken)

Repository chỉ cho Aggregate Root.
```

---

## PHASE 3: BACKEND — INFRASTRUCTURE

### Prompt 3.1 — NuGet Packages

```
Cài NuGet packages cho MinimalAPI backend:

MinimalAPI.Infrastructure:
- Microsoft.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL
- Microsoft.EntityFrameworkCore.Design

MinimalAPI.Application:
- MediatR, FluentValidation, FluentValidation.DependencyInjection

MinimalAPI.Api:
- Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File, Serilog.Sinks.Seq
- Serilog.Enrichers.Environment, Serilog.Enrichers.Process
- Swashbuckle.AspNetCore
- Microsoft.EntityFrameworkCore.Design

MinimalAPI.Domain: KHÔNG cài gì.

Dùng dotnet add package. Chạy dotnet restore && dotnet build verify.
```

### Prompt 3.2 — DbContext & EF Core Configurations

```
Tạo EF Core trong MinimalAPI.Infrastructure/Persistence/:

1. AppDbContext.cs:
   - Primary constructor(DbContextOptions, IMediator)
   - DbSet<Product>, DbSet<Category>
   - Override SaveChangesAsync: commit → collect domain events → clear → dispatch via MediatR
   - OnModelCreating: ApplyConfigurationsFromAssembly

2. Configurations/ProductConfiguration.cs:
   - Table "products", snake_case columns
   - HasConversion: ProductId ↔ Guid, CategoryId ↔ Guid
   - ComplexProperty: ProductName → column "name" (max 200)
   - ComplexProperty: Money → columns "price_amount" decimal(18,2), "price_currency" varchar(3)
   - HasConversion<string> cho enum nếu có
   - Ignore DomainEvents

3. Configurations/CategoryConfiguration.cs:
   - Table "categories", snake_case
   - HasConversion: CategoryId ↔ Guid
   - Name: required, max 100, unique index
   - Ignore DomainEvents
```

### Prompt 3.3 — Repositories & DI Registration

```
Tạo trong MinimalAPI.Infrastructure/:

Persistence/Repositories/:
1. ProductRepository.cs — implement IProductRepository, primary constructor(AppDbContext)
2. CategoryRepository.cs — implement ICategoryRepository
3. UnitOfWork.cs — implement IUnitOfWork, delegate to AppDbContext

DependencyInjection.cs:
- Static method AddInfrastructure(this IServiceCollection, IConfiguration)
- Đăng ký: AppDbContext (UseNpgsql, connection string "DefaultConnection")
- Đăng ký: IProductRepository, ICategoryRepository, IUnitOfWork
- Đăng ký: IApplicationDbContext → AppDbContext (cho query side)
```

### Prompt 3.4 — Environments & Migration

```
Cấu hình 2 môi trường cho MinimalAPI.Api và tạo migration đầu tiên:

1. appsettings.json (shared config):
   - Serilog base config: MinimumLevel Information
   - Không chứa connection string

2. appsettings.Development.json:
   - ConnectionStrings:DefaultConnection = "Host=localhost;Port=5432;Database=minimalapi_dev;Username=postgres;Password=postgres123"
   - Serilog: WriteTo Console + File (logs/log-.txt, rolling daily)
   - Serilog MinimumLevel.Override: Microsoft.AspNetCore = Warning
   - EnableSwagger: true

3. appsettings.Production.json:
   - ConnectionStrings:DefaultConnection = "Host=db;Port=5432;Database=minimalapi_prod;Username=postgres;Password=OVERRIDE_VIA_ENV"
   - Serilog: WriteTo File only (logs/log-.txt), MinimumLevel Warning
   - EnableSwagger: false

4. Properties/launchSettings.json:
   - Profile "dev": ASPNETCORE_ENVIRONMENT = Development, port 5000
   - Profile "prod": ASPNETCORE_ENVIRONMENT = Production, port 8080

5. Tạo migration:
   - dotnet ef migrations add InitialCreate --project src/MinimalAPI.Infrastructure --startup-project src/MinimalAPI.Api
   - dotnet ef database update --startup-project src/MinimalAPI.Api
   - Verify bảng trong PostgreSQL: docker exec psql -c "\dt"
```

---

## PHASE 4: BACKEND — APPLICATION LAYER

### Prompt 4.1 — Pipeline Behaviors + Result Pattern

```
Tạo trong MinimalAPI.Application/:

Behaviors/:
1. ValidationBehavior.cs — IPipelineBehavior, inject IEnumerable<IValidator<TRequest>>
   Nếu có validator và lỗi → throw ValidationException. Primary constructor.
2. LoggingBehavior.cs — IPipelineBehavior, log request name + elapsed time (Stopwatch)

Abstractions/:
1. Result.cs — record Result<T>: Value, Error, IsSuccess. Static: Success(T), Failure(string)
2. PagedResult.cs — record PagedResult<T>: Items, TotalCount, Page, PageSize, TotalPages, HasNext, HasPrevious

DependencyInjection.cs:
- AddApplication(this IServiceCollection)
- Đăng ký MediatR + behaviors (Validation trước, Logging sau) + validators from assembly
```

### Prompt 4.2 — Feature: Product CRUD (VSA)

```
Tạo CRUD features cho Product trong MinimalAPI.Application/Features/Products/:

DTOs/ProductResponse.cs:
- record(Guid Id, string Name, decimal Price, string Currency, Guid CategoryId, string CategoryName, string? Description, bool IsActive, DateTime CreatedAt)

CreateProduct/ (Command side — EF Core):
- CreateProductCommand.cs: record(string Name, decimal Price, string Currency, Guid CategoryId, string? Description) : IRequest<Result<Guid>>
- CreateProductValidator.cs: Name NotEmpty MaxLength(200), Price > 0, Currency NotEmpty Length(3), CategoryId NotEmpty. Lỗi tiếng Việt.
- CreateProductHandler.cs: check category exists → tạo Product.Create() → repo.Add → unitOfWork.Save → return Success(id)

GetProduct/ (Query side — EF Core LINQ):
- GetProductQuery.cs: record(Guid Id) : IRequest<ProductResponse?>
- GetProductHandler.cs: inject IApplicationDbContext, LINQ Join Products + Categories → ProjectDto

GetProducts/ (Query side — EF Core LINQ):
- GetProductsQuery.cs: record(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<ProductResponse>>
- GetProductsHandler.cs: LINQ Join + search filter + CountAsync + Skip/Take

UpdateProduct/:
- UpdateProductCommand, Validator, Handler: lấy product → UpdateInfo + UpdatePrice → save

DeleteProduct/:
- DeleteProductCommand, Handler: lấy product → repo.Remove → save

Mỗi feature 1 folder chứa tất cả files liên quan.
```

### Prompt 4.3 — Feature: Category CRUD (VSA)

```
Tạo CRUD features cho Category trong MinimalAPI.Application/Features/Categories/:

DTOs/CategoryResponse.cs:
- record(Guid Id, string Name, string? Description, DateTime CreatedAt)

CreateCategory/:
- Command + Validator (Name NotEmpty, MaxLength 100, kiểm tra trùng tên) + Handler

GetCategories/:
- Query(int Page = 1, int PageSize = 20) + Handler (EF Core LINQ)

UpdateCategory/:
- Command + Validator + Handler

DeleteCategory/:
- Command + Handler (check không có product nào thuộc category trước khi xóa)

Giữ đơn giản hơn Product.
```

---

## PHASE 5: BACKEND — API LAYER

### Prompt 5.1 — Program.cs (Dev + Prod)

```
Cấu hình MinimalAPI.Api/Program.cs với 2 môi trường:

1. Serilog: UseSerilog() đọc config từ appsettings (đã tạo ở 3.4)

2. Services:
   - AddApplication() + AddInfrastructure(config)
   - AddProblemDetails
   - AddEndpointsApiExplorer + AddSwaggerGen (title "MinimalAPI", version "v1")
   - AddCors: policy "frontend"

3. Middleware pipeline:
   - UseExceptionHandler: ValidationException → 400, DomainException → 400, _ → 500 (ProblemDetails format)
   - UseCors("frontend")
   - if Development: UseSwagger + UseSwaggerUI
   - Map endpoints

4. CORS policy:
   - Development: AllowOrigins "http://localhost:4200", AllowAnyHeader, AllowAnyMethod
   - Production: AllowOrigins từ config "AllowedOrigins"

5. Seed data (Development only):
   - if app.Environment.IsDevelopment() → scope → SeedData.SeedAsync(db)

6. Verify: dotnet run → Swagger UI tại /swagger
```

### Prompt 5.2 — Endpoints + Seed Data

```
Tạo Minimal API endpoints và seed data:

MinimalAPI.Api/Endpoints/:
1. ProductEndpoints.cs:
   - MapProductEndpoints(this WebApplication)
   - Group: /api/products, tag "Products"
   - GET /, GET /{id:guid}, POST /, PUT /{id:guid}, DELETE /{id:guid}
   - Inject ISender (MediatR), dùng TypedResults
   - Result handling: Success → Ok/Created, Failure → NotFound/BadRequest
   - WithName, WithSummary, Produces<> cho Swagger

2. CategoryEndpoints.cs:
   - Group: /api/categories, tag "Categories"
   - CRUD endpoints tương tự

3. Đăng ký trong Program.cs: app.MapProductEndpoints(), app.MapCategoryEndpoints()

MinimalAPI.Infrastructure/Persistence/SeedData.cs:
- SeedAsync(AppDbContext db): nếu chưa có data → tạo 3 categories + 5 products
- Dùng factory method Create() của entity

Chạy project, test tất cả endpoints qua Swagger. Kiểm tra Serilog logs trong console + file.
Nếu có lỗi thì fix cho đến khi mọi thứ hoạt động.
```

---

## PHASE 6: FRONTEND — ANGULAR

### Prompt 6.1 — Cấu trúc, Models, Services

```
Setup Angular project tại D:\Projects\MinimalAPI\frontend:

1. Cài Bootstrap: npm install bootstrap
   Import vào styles.scss: @import "bootstrap/scss/bootstrap";

2. Cấu trúc:
   src/app/
   ├── core/
   │   ├── services/ (product.service.ts, category.service.ts)
   │   ├── models/ (product.model.ts, category.model.ts, paged-result.model.ts)
   │   └── interceptors/ (error.interceptor.ts)
   ├── features/
   │   ├── products/ (list + form components)
   │   ├── categories/ (list + form components)
   │   └── home/ (home component)
   └── shared/components/

3. Models (TypeScript interfaces):
   - Product: id, name, price, currency, categoryId, categoryName, description?, isActive, createdAt
   - Category: id, name, description?, createdAt
   - PagedResult<T>: items, totalCount, page, pageSize, totalPages

4. Environment: src/environments/environment.ts → apiUrl = 'http://localhost:5000/api'

5. Services (HttpClient, return Observable):
   - ProductService: getAll(page, pageSize, search?), getById(id), create(data), update(id, data), delete(id)
   - CategoryService: getAll(), create(data), update(id, data), delete(id)

6. provideHttpClient() trong app.config.ts
```

### Prompt 6.2 — Product & Category Pages

```
Tạo các trang UI trong Angular:

1. ProductListComponent (features/products/product-list/):
   - Bảng: tên, giá, category, trạng thái (Active/Inactive)
   - Phân trang Previous/Next
   - Ô tìm kiếm theo tên
   - Nút: Thêm, Sửa, Xóa (confirm trước khi xóa)
   - Bootstrap table

2. ProductFormComponent (features/products/product-form/):
   - Dùng chung Create + Edit (detect qua route param id)
   - Reactive Form: Name (required, max 200), Price (required, > 0), Currency (dropdown VND/USD), Category (dropdown), Description
   - Validation errors inline
   - Submit → navigate về /products

3. CategoryListComponent + CategoryFormComponent:
   - Đơn giản: chỉ Name + Description
   - CRUD tương tự Product

4. HomeComponent: trang chào "MinimalAPI — Hệ thống quản lý sản phẩm"

5. Navbar: links Home, Products, Categories

6. Routing:
   / → Home
   /products → list, /products/create → form, /products/edit/:id → form
   /categories → list, /categories/create → form, /categories/edit/:id → form
   ** → 404

7. Error interceptor: catch 400 (show validation errors), 404, 500
8. Loading state + disable submit khi đang gửi

Giao diện clean, đơn giản. Ưu tiên hoạt động đúng.
```

---

## PHASE 7: TÍCH HỢP & HOÀN THIỆN

### Prompt 7.1 — Kết nối BE + FE

```
Kết nối Backend và Frontend:

1. Verify CORS: BE cho phép http://localhost:4200
2. Verify API URL: FE environment.ts trỏ đúng port BE
3. Chạy đồng thời:
   - Terminal 1: dotnet run --project backend/src/MinimalAPI.Api
   - Terminal 2: cd frontend && ng serve
4. Test qua UI tại http://localhost:4200:
   - CRUD categories
   - CRUD products (chọn category từ dropdown)
   - Phân trang, tìm kiếm
5. Fix lỗi nếu có (CORS, serialization, routing...)
```

### Prompt 7.2 — Docker Compose (Dev + Prod)

```
Tạo Docker setup cho MinimalAPI:

1. backend/Dockerfile (multi-stage):
   - Stage build: mcr.microsoft.com/dotnet/sdk:10.0-preview, restore → publish
   - Stage runtime: mcr.microsoft.com/dotnet/aspnet:10.0-preview, expose 8080, non-root user
   - Comment giải thích từng stage

2. frontend/Dockerfile (multi-stage):
   - Stage build: node, npm install → ng build --configuration production
   - Stage runtime: nginx, serve static + proxy /api → api:8080
   - frontend/nginx.conf: proxy_pass cho /api

3. docker-compose.dev.yml (đã có từ Phase 0, PostgreSQL + Seq + API):
   - PostgreSQL: container_name MinimalAPI.PostgresDB
   - Seq: container_name MinimalAPI.Seq, port 5341 (ingestion) + 8081 (UI)
   - API: build từ backend/Dockerfile, port 5000:8080, depends_on db healthy
   - Override Seq URL qua env: Serilog__WriteTo__2__Args__serverUrl=http://seq:5341

4. docker-compose.yml (Production — full stack):
   services:
     db: postgres:17, volume pgdata, healthcheck
       container_name: MinimalAPI.PostgresDB
       POSTGRES_DB: minimalapi_prod
     api: build ./backend
       container_name: MinimalAPI.Host
       ASPNETCORE_ENVIRONMENT: Production
       depends_on db healthy
       ports: 5000:8080
     web: build ./frontend
       container_name: MinimalAPI.Angular
       depends_on api
       ports: 80:80

5. .dockerignore cho cả backend và frontend

6. Test: docker compose up --build
   - http://localhost → FE
   - http://localhost:5000/swagger → KHÔNG hiện (Production)
```

### Prompt 7.3 — Hoàn thiện & Documentation

```
Hoàn thiện project MinimalAPI:

1. Kiểm tra conventions:
   - Domain: pure C#, không NuGet
   - snake_case DB columns
   - Primary constructor, TypedResults, VSA folders
   - Development vs Production config đúng

2. .gitignore: bin/, obj/, node_modules/, dist/, logs/, .vs/, .idea/, *.user, docker override

3. README.md tại root:
   - Mô tả project + tech stack
   - Cấu trúc thư mục (tree ngắn gọn)
   - Hướng dẫn chạy:
     a. Prerequisites (Docker, .NET 10, Node.js)
     b. Development: docker compose -f docker-compose.dev.yml up -d (DB) → dotnet run → ng serve
     c. Production: docker compose up --build
   - API docs: Swagger tại /swagger (chỉ Development)
   - Hướng dẫn intern: đọc code từ Domain → Infrastructure → Application → Api

4. Verify:
   - dotnet build — không warning
   - ng build — không error
   - docker compose up --build — cả 3 services chạy OK
```

---

## TÓM TẮT

```
Phase 0  (1 prompt)   Môi trường: Angular CLI + PostgreSQL Docker
Phase 1  (3 prompts)  Khởi tạo: BE solution + FE Angular + Claude CLI
Phase 2  (4 prompts)  Domain: Primitives → IDs + VOs → Entities → Repo interfaces
Phase 3  (4 prompts)  Infrastructure: NuGet → DbContext → Repos → Environments + Migration
Phase 4  (3 prompts)  Application: Behaviors + Result → Product CRUD → Category CRUD
Phase 5  (2 prompts)  API: Program.cs → Endpoints + Seed
Phase 6  (2 prompts)  Frontend: Setup + Services → Pages + Routing
Phase 7  (3 prompts)  Tích hợp: Connect → Docker → Documentation

TỔNG: 22 prompts
```

## LƯU Ý

- Mỗi prompt xong → verify build pass trước khi tiếp
- Domain viết trước — foundation của mọi thứ
- Không Redis, không Kafka
- 2 môi trường: Development (Swagger, logs, seed) vs Production (no Swagger, minimal logs)
- Claude CLI tối giản: chỉ CLAUDE.md + 2 agents + 3 commands → tiết kiệm token mỗi session
