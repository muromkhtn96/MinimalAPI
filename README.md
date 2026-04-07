# MinimalAPI

Hệ thống quản lý sản phẩm (Product Management) xây dựng trên .NET 10 Minimal API theo kiến trúc DDD + Vertical Slice Architecture.

## Tech Stack

- **Backend**: .NET 10 Minimal API, DDD (4 layers), VSA, MediatR, FluentValidation
- **Database**: PostgreSQL 17 (EF Core)
- **Logging**: Serilog (Console, File, Seq)
- **API Docs**: Swagger/OpenAPI (Development only)
- **Containerization**: Docker, Docker Compose

## Cấu trúc thư mục

```
MinimalAPI/
├── backend/
│   ├── src/
│   │   ├── MinimalAPI.Domain/           # Pure C#, entities, value objects
│   │   ├── MinimalAPI.Application/      # Features (VSA), MediatR handlers
│   │   ├── MinimalAPI.Infrastructure/   # EF Core, repositories, migrations
│   │   └── MinimalAPI.Api/              # Endpoints, Program.cs, Serilog
│   ├── Dockerfile
│   └── MinimalAPI.slnx
├── docs/
│   └── prompts-guide.md
├── docker-compose.dev.yml              # Dev: PostgreSQL + Seq + API + pgAdmin
├── docker-compose.yml                  # Prod: PostgreSQL + API
└── README.md
```

## Architecture

```
Api → Application → Domain ← Infrastructure
```

- **Domain**: Pure C#, không dependency — Entities, Value Objects, Domain Events
- **Application**: MediatR + FluentValidation, CQRS (Command = EF Core tracked, Query = EF Core LINQ AsNoTracking)
- **Infrastructure**: EF Core (Npgsql provider), Repositories, Migrations
- **Api**: Minimal API endpoints, Serilog, Swagger

## Chạy Development (Docker)

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (nếu chạy backend ngoài Docker)

### Option 1: Full Docker (API + DB + Seq + pgAdmin)

```bash
docker compose -f docker-compose.dev.yml up --build
```

Sau khi chạy xong:

| Service    | URL                          | Mô tả                    |
|------------|------------------------------|---------------------------|
| API        | http://localhost:5000        | Backend API               |
| Swagger UI | http://localhost:5000/swagger | API documentation         |
| Seq UI     | http://localhost:8081        | Xem structured logs       |
| pgAdmin    | http://localhost:5050        | PostgreSQL web UI         |
| PostgreSQL | localhost:5432               | Database (TCP, không có web UI) |

### Option 2: Chỉ DB + Seq (chạy API local)

```bash
# Khởi động PostgreSQL + Seq
docker compose -f docker-compose.dev.yml up db seq -d

# Chạy API local
dotnet run --project backend/src/MinimalAPI.Api
```

API chạy tại http://localhost:5000/swagger

## Xem Logs (Seq)

1. Mở http://localhost:8081
2. Tất cả structured logs từ API sẽ hiển thị realtime
3. Có thể filter theo level, search theo message, property

Khi chạy local (không Docker), Seq nhận logs tại `http://localhost:5341`.
Khi chạy trong Docker, API gửi logs đến `http://seq:5341` (Docker network).

## API Endpoints

### Products (`/api/products`)

| Method | Path                | Mô tả              |
|--------|---------------------|---------------------|
| GET    | `/api/products`     | Danh sách sản phẩm  |
| GET    | `/api/products/{id}`| Chi tiết sản phẩm   |
| POST   | `/api/products`     | Tạo sản phẩm mới    |
| PUT    | `/api/products/{id}`| Cập nhật sản phẩm   |
| DELETE | `/api/products/{id}`| Xóa sản phẩm        |

### Categories (`/api/categories`)

| Method | Path                   | Mô tả              |
|--------|------------------------|---------------------|
| GET    | `/api/categories`      | Danh sách danh mục  |
| GET    | `/api/categories/{id}` | Chi tiết danh mục   |
| POST   | `/api/categories`      | Tạo danh mục mới    |
| PUT    | `/api/categories/{id}` | Cập nhật danh mục   |
| DELETE | `/api/categories/{id}` | Xóa danh mục        |

## Chạy Production (Docker)

```bash
# Tạo file .env với secrets
echo "DB_PASSWORD=your_secure_password" > .env
echo "ALLOWED_ORIGINS=https://yourdomain.com" >> .env

# Deploy
docker compose up --build -d
```

| Service    | URL                    | Mô tả              |
|------------|------------------------|---------------------|
| API        | http://localhost:5000  | Backend API         |
| Health     | http://localhost:5000/health | Health check   |
| PostgreSQL | localhost:5432         | Database            |

**Lưu ý Production:**
- Swagger **tắt** — không expose API docs
- HTTPS redirect **bật** — cần reverse proxy (nginx/traefik) phía trước
- Rate limiting: 100 requests/phút/IP
- DB retry: tự động retry 3 lần khi lỗi tạm thời
- Migrations tự chạy khi startup

## Conventions

- Primary constructor cho DI
- `Result<T>` pattern (không throw cho business logic)
- `TypedResults` cho endpoints
- Strongly Typed IDs (`readonly record struct`)
- Value Objects: `sealed record` + private ctor + static `Create()`
- DB: snake_case (bảng + cột)
- CQRS: Command = EF Core (tracked), Query = EF Core LINQ (AsNoTracking)
