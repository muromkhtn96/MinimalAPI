# MinimalAPI

Hệ thống quản lý sản phẩm (Product Management) full-stack xây dựng trên .NET 10 Minimal API theo kiến trúc DDD + Vertical Slice Architecture với Angular 19 frontend.

---

## ⚡ TL;DR - ONE COMMAND START

**Chỉ cần 1 lệnh để chạy TẤT CẢ (DB + API + Frontend + Seq + pgAdmin):**

```bash
docker compose -f docker-compose.dev.yml up --build -d
```

**Sau đó truy cập:**
- 🎨 **Frontend**: http://localhost:4200
- 📖 **Swagger**: http://localhost:5000/swagger
- 📊 **Seq Logs**: http://localhost:8081
- 🗄️ **pgAdmin**: http://localhost:5050

**Stop tất cả:**
```bash
docker compose -f docker-compose.dev.yml down
```

---

## 🚀 Tech Stack

- **Backend**: .NET 10 Minimal API, DDD (4 layers), VSA, MediatR, FluentValidation
- **Frontend**: Angular 19, Bootstrap 5, RxJS, Reactive Forms
- **Database**: PostgreSQL 17 (EF Core, Npgsql)
- **Logging**: Serilog → Console + File + Seq
- **API Docs**: Swagger/OpenAPI (Development only)
- **Containerization**: Docker, Docker Compose
- **Dev Tools**: pgAdmin 4, Seq Dashboard

## 📁 Cấu trúc thư mục

```
MinimalAPI/
├── backend/
│   ├── src/
│   │   ├── MinimalAPI.Domain/           # Pure C#, entities, value objects, events
│   │   ├── MinimalAPI.Application/      # Features (VSA), MediatR handlers, validators
│   │   ├── MinimalAPI.Infrastructure/   # EF Core, repositories, migrations, seed data
│   │   └── MinimalAPI.Api/              # Minimal API endpoints, Serilog, Swagger
│   ├── Dockerfile                       # Multi-stage: SDK → Runtime
│   └── MinimalAPI.slnx
├── frontend/
│   ├── src/app/
│   │   ├── core/                       # Models, Services, Interceptors
│   │   ├── features/                   # Product & Category components
│   │   └── shared/                     # Shared components
│   ├── Dockerfile                       # Multi-stage: Node → nginx
│   └── nginx.conf                       # Reverse proxy config
├── docs/
│   └── prompts-guide.md                # Step-by-step build guide
├── docker-compose.dev.yml              # Dev: DB + Seq + API + Web + pgAdmin (5 services)
├── docker-compose.yml                  # Prod: DB + API + Web (3 services)
├── .env.example                        # Template for environment variables
├── CLAUDE.md                           # Project conventions & rules
└── README.md
```

## 🏗️ Architecture

### Backend - 4 Layer DDD

```
┌─────────────┐
│     Api     │  ← Minimal API endpoints, Serilog, Swagger, CORS
└──────┬──────┘
       │
┌──────▼──────────┐
│  Application    │  ← MediatR, FluentValidation, Features (VSA)
└──────┬──────────┘
       │
┌──────▼──────┐
│   Domain    │  ← Entities, VOs, Events, Repo interfaces (Pure C#)
└──────▲──────┘
       │
┌──────┴──────────┐
│ Infrastructure  │  ← EF Core, Repositories, Migrations, Seed
└─────────────────┘
```

**Nguyên tắc:**
- **Domain**: Pure C#, zero dependencies
- **Application**: CQRS pattern (Commands = tracked, Queries = AsNoTracking)
- **Infrastructure**: EF Core với snake_case DB schema
- **Api**: TypedResults, Result<T> pattern, không throw exceptions cho business logic

### Frontend - Angular 19

```
┌──────────────────┐
│   Components     │  ← Smart + Presentational components
└────────┬─────────┘
         │
┌────────▼─────────┐
│    Services      │  ← HttpClient, RxJS Observables
└────────┬─────────┘
         │
┌────────▼─────────┐
│  Interceptors    │  ← Error handling, headers
└──────────────────┘
```

## 🐳 Quick Start - Development (Full Stack)

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac/Linux)
- **Optional** (chỉ cần nếu chạy local):
  - [.NET 10 SDK](https://dotnet.microsoft.com/download)
  - [Node.js 20+](https://nodejs.org/) và npm
  - [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`

### Option 1: Full Docker Stack (Recommended) ⭐

**Chỉ cần 1 lệnh để chạy TẤT CẢ 5 services:**

```bash
# Build và start: DB + Seq + API + Frontend + pgAdmin
docker compose -f docker-compose.dev.yml up --build -d
```

**Lệnh này sẽ tự động:**
1. ✅ Build Backend (.NET 10) từ source code
2. ✅ Build Frontend (Angular 19) từ source code
3. ✅ Start PostgreSQL database
4. ✅ Start Seq logging server
5. ✅ Start pgAdmin web UI
6. ✅ Run database migrations
7. ✅ Seed sample data (3 categories + 5 products)

**Quản lý containers:**

```bash
# Xem logs realtime của tất cả services
docker compose -f docker-compose.dev.yml logs -f

# Xem status của tất cả containers
docker compose -f docker-compose.dev.yml ps

# Stop tất cả services
docker compose -f docker-compose.dev.yml down

# Stop và xóa volumes (reset database)
docker compose -f docker-compose.dev.yml down -v
```

**Services được khởi động:**

| Service | Container | URL | Mô tả |
|---------|-----------|-----|-------|
| 🎨 **Frontend** | Web | http://localhost:4200 | Angular 19 UI |
| ⚙️ **Backend** | Api | http://localhost:5000 | .NET 10 Minimal API |
| 📖 **Swagger** | Api | http://localhost:5000/swagger | API Documentation |
| 📊 **Seq** | SeqLog | http://localhost:8081 | Structured Logs Dashboard |
| 🗄️ **pgAdmin** | PgAdmin | http://localhost:5050 | PostgreSQL Web UI |
| 💾 **PostgreSQL** | PostgresDB | localhost:5432 | Database (TCP only) |

**Seed Data (Development):**
- ✅ 3 Categories: Điện tử, Thời trang, Sách
- ✅ 5 Products: Laptop Dell XPS 13, iPhone 15 Pro, Áo thun nam, Giày Nike, Clean Code

**pgAdmin Credentials:**
- Email: `admin@admin.com`
- Password: `admin123`

**Database Connection (trong pgAdmin):**
- Host: `db` (hoặc `localhost` nếu connect từ máy local)
- Port: `5432`
- Database: `minimalapi_dev`
- Username: `postgres`
- Password: `postgres123`

### Option 2: Hybrid (DB + Seq in Docker, API + FE local)

Tốt cho debugging và hot-reload nhanh:

```bash
# 1. Start infrastructure services
docker compose -f docker-compose.dev.yml up db seq -d

# 2. Terminal 1: Run backend
dotnet run --project backend/src/MinimalAPI.Api

# 3. Terminal 2: Run frontend
cd frontend
npm install
ng serve
```

**Access:**
- Frontend: http://localhost:4200 (hot-reload enabled)
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Seq: http://localhost:8081

## 📝 Common Docker Commands

### Chạy Full Stack (1 lệnh duy nhất)

```bash
# Start tất cả (DB + API + Frontend + Seq + pgAdmin)
docker compose -f docker-compose.dev.yml up --build -d
```

### Quản lý Containers

```bash
# Xem status tất cả containers
docker compose -f docker-compose.dev.yml ps

# Xem logs tất cả services
docker compose -f docker-compose.dev.yml logs -f

# Xem logs của 1 service cụ thể
docker compose -f docker-compose.dev.yml logs -f api      # Backend
docker compose -f docker-compose.dev.yml logs -f web      # Frontend
docker compose -f docker-compose.dev.yml logs -f db       # Database
docker compose -f docker-compose.dev.yml logs -f seq      # Seq Logs
```

### Restart Services

```bash
# Restart tất cả
docker compose -f docker-compose.dev.yml restart

# Restart 1 service cụ thể
docker compose -f docker-compose.dev.yml restart api      # Backend only
docker compose -f docker-compose.dev.yml restart web      # Frontend only
```

### Stop Services

```bash
# Stop tất cả (giữ data)
docker compose -f docker-compose.dev.yml down

# Stop và XÓA volumes (reset database - MẤT DATA!)
docker compose -f docker-compose.dev.yml down -v
```

### Rebuild Sau Khi Sửa Code

```bash
# Rebuild tất cả
docker compose -f docker-compose.dev.yml up --build -d

# Rebuild chỉ Backend (sau khi sửa C# code)
docker compose -f docker-compose.dev.yml up --build -d api

# Rebuild chỉ Frontend (sau khi sửa Angular code)
docker compose -f docker-compose.dev.yml up --build -d web
```

### Troubleshooting

```bash
# Xem resource usage
docker stats

# Exec vào container để debug
docker compose -f docker-compose.dev.yml exec api bash        # Backend
docker compose -f docker-compose.dev.yml exec web sh          # Frontend (Alpine)
docker compose -f docker-compose.dev.yml exec db psql -U postgres -d minimalapi_dev  # Database

# Xóa tất cả containers, networks, images không dùng
docker system prune -a
```

## 🌐 API Endpoints

### Products

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| GET | `/api/products` | Danh sách sản phẩm (phân trang, tìm kiếm) | - |
| GET | `/api/products/{id}` | Chi tiết sản phẩm | - |
| POST | `/api/products` | Tạo sản phẩm mới | `CreateProductRequest` |
| PUT | `/api/products/{id}` | Cập nhật sản phẩm | `UpdateProductRequest` |
| DELETE | `/api/products/{id}` | Xóa sản phẩm | - |

**Query Parameters (GET /api/products):**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `search` (string, optional) - Tìm kiếm theo tên

### Categories

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | Danh sách danh mục |
| GET | `/api/categories/{id}` | Chi tiết danh mục |
| POST | `/api/categories` | Tạo danh mục mới |
| PUT | `/api/categories/{id}` | Cập nhật danh mục |
| DELETE | `/api/categories/{id}` | Xóa danh mục |

### Health Check

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Database health check |

## 📊 Xem Logs với Seq

1. Truy cập http://localhost:8081
2. Xem tất cả structured logs từ API realtime
3. **Features:**
   - Filter theo level (Information, Warning, Error)
   - Search theo message hoặc properties
   - Tracing requests với TraceId
   - Aggregate queries

**Serilog Sinks:**
- **Console**: Logs hiển thị trong terminal
- **File**: `backend/src/MinimalAPI.Api/logs/log-{Date}.txt`
- **Seq**: http://seq:5341 (Docker) hoặc http://localhost:5341 (local)

## 🚢 Production Deployment

### Build và Deploy

```bash
# 1. Tạo .env file với production secrets
cat > .env << EOF
DB_PASSWORD=your_secure_password_here
ALLOWED_ORIGINS=http://localhost:8080
EOF

# 2. Build và start production stack
docker compose up --build -d

# 3. Verify all services
docker compose ps
docker compose logs -f
```

**Production Stack (3 services):**

| Service | URL | Mô tả |
|---------|-----|-------|
| 🌐 **Web (nginx)** | http://localhost:8080 | Frontend + API Proxy |
| ⚙️ **API** | http://localhost:5000 | Backend (direct access) |
| 💾 **PostgreSQL** | localhost:5432 | Database |

**Production Features:**
- ✅ Swagger **disabled**
- ✅ HTTPS redirect enabled
- ✅ Rate limiting: 100 req/min per IP
- ✅ Auto migrations on startup
- ✅ Health checks với retry logic
- ✅ Multi-stage Docker builds (optimized image size)
- ✅ nginx reverse proxy: `/api/*` → backend
- ❌ **NO** seed data (production DB should be empty initially)

**nginx Reverse Proxy:**
- Static files (Angular): `http://localhost:8080/*`
- API requests: `http://localhost:8080/api/*` → `http://api:8080/api/*`

## 🔧 Container Management

### View Status

```bash
# Development
docker compose -f docker-compose.dev.yml ps

# Production
docker compose ps
```

### View Logs

```bash
# All services
docker compose -f docker-compose.dev.yml logs -f

# Specific service
docker compose -f docker-compose.dev.yml logs -f api
docker compose -f docker-compose.dev.yml logs -f web
docker compose -f docker-compose.dev.yml logs -f db
```

### Restart Services

```bash
# Restart all
docker compose -f docker-compose.dev.yml restart

# Restart specific service
docker compose -f docker-compose.dev.yml restart api
docker compose -f docker-compose.dev.yml restart web
```

### Rebuild After Code Changes

```bash
# Rebuild all services
docker compose -f docker-compose.dev.yml up --build -d

# Rebuild specific service
docker compose -f docker-compose.dev.yml up --build -d api
docker compose -f docker-compose.dev.yml up --build -d web
```

### Clean Up

```bash
# Stop và xóa containers (giữ volumes)
docker compose -f docker-compose.dev.yml down

# Stop, xóa containers VÀ volumes (reset database)
docker compose -f docker-compose.dev.yml down -v
```

## 🧪 Testing

### Manual Testing via Swagger

1. Mở http://localhost:5000/swagger
2. Explore API endpoints
3. Try out requests với interactive UI

### Manual Testing via Frontend

1. Mở http://localhost:4200
2. Test CRUD operations:
   - ➕ Create new categories/products
   - 📝 Update existing items
   - 🗑️ Delete items
   - 🔍 Search products by name
   - 📄 Pagination

### cURL Examples

```bash
# Get all categories
curl http://localhost:5000/api/categories

# Get products with pagination
curl "http://localhost:5000/api/products?page=1&pageSize=5"

# Search products
curl "http://localhost:5000/api/products?search=laptop"

# Create category
curl -X POST http://localhost:5000/api/categories \
  -H "Content-Type: application/json" \
  -d '{"name":"Đồ gia dụng","description":"Các sản phẩm gia dụng"}'

# Create product
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name":"MacBook Pro",
    "price":45000000,
    "currency":"VND",
    "categoryId":"<CATEGORY_ID_HERE>",
    "description":"Laptop cao cấp"
  }'
```

## 📚 Project Conventions

### Code Style

- **Primary constructors** cho dependency injection
- **Result<T> pattern** thay vì throw exceptions cho business logic
- **TypedResults** cho API endpoints
- **Strongly Typed IDs**: `readonly record struct ProductId(Guid Value)`
- **Value Objects**: `sealed record` + private ctor + static `Create()`
- **Vietnamese** user-facing messages, **English** code

### Database

- **Tables**: lowercase plural, snake_case (e.g., `products`, `categories`)
- **Columns**: snake_case (e.g., `created_at`, `price_amount`)
- **Migrations**: EF Core Code-First
- **Seeding**: Development only, idempotent

### CQRS Pattern

- **Commands**: Use EF Core with change tracking → `SaveChangesAsync()`
- **Queries**: Use EF Core LINQ with `AsNoTracking()` → better performance

### Frontend

- **Components**: Smart (container) + Presentational (dumb)
- **Services**: HttpClient với RxJS Observables
- **Forms**: Reactive Forms với validation
- **Error Handling**: Global error interceptor
- **State**: Component-level (no NgRx for this size)

## 🐛 Troubleshooting

### Port already in use

```bash
# Windows: Tìm và kill process đang dùng port
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/Mac
lsof -ti:5000 | xargs kill -9
```

### Database connection failed

```bash
# Kiểm tra PostgreSQL container
docker compose -f docker-compose.dev.yml logs db

# Restart database
docker compose -f docker-compose.dev.yml restart db

# Reset database (XÓA TẤT CẢ DATA!)
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up db -d
```

### Frontend không hiển thị data

```bash
# Kiểm tra API health
curl http://localhost:5000/health

# Kiểm tra CORS headers
curl -I -X OPTIONS http://localhost:5000/api/products \
  -H "Origin: http://localhost:4200"

# Xem browser console logs (F12)
```

### Seq không nhận logs

```bash
# Kiểm tra Seq container
docker compose -f docker-compose.dev.yml logs seq

# Verify API có gửi logs không
docker compose -f docker-compose.dev.yml logs api | grep Seq

# Check Seq URL trong API
docker compose -f docker-compose.dev.yml exec api printenv | grep Serilog
```

### Build failed - Angular

```bash
# Clear npm cache
cd frontend
rm -rf node_modules package-lock.json
npm install

# Clear Angular cache
rm -rf .angular dist

# Rebuild
npm run build
```

### Build failed - Backend

```bash
# Clean solution
cd backend
dotnet clean

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

## 📖 Documentation

- **API Docs**: http://localhost:5000/swagger (Development only)
- **Architecture Guide**: `CLAUDE.md`
- **Build Guide**: `docs/prompts-guide.md` (Step-by-step từ zero)
- **Serilog Docs**: https://serilog.net/
- **Angular Docs**: https://angular.dev/

## 🤝 Contributing

1. Đọc `CLAUDE.md` để hiểu conventions
2. Tạo feature branch từ `main`
3. Code theo DDD + VSA pattern
4. Test local với `docker-compose.dev.yml`
5. Tạo PR về `main`

## 📋 Quick Reference Card

### 🚀 Start Everything (1 Command)
```bash
docker compose -f docker-compose.dev.yml up --build -d
```

### 🔗 Access URLs
```
Frontend:  http://localhost:4200
Swagger:   http://localhost:5000/swagger
Seq Logs:  http://localhost:8081
pgAdmin:   http://localhost:5050 (admin@admin.com / admin123)
```

### 📊 View Status & Logs
```bash
docker compose -f docker-compose.dev.yml ps                    # Status
docker compose -f docker-compose.dev.yml logs -f               # All logs
docker compose -f docker-compose.dev.yml logs -f api           # API only
```

### 🔄 Restart After Code Changes
```bash
docker compose -f docker-compose.dev.yml up --build -d api     # Backend
docker compose -f docker-compose.dev.yml up --build -d web     # Frontend
```

### 🛑 Stop Everything
```bash
docker compose -f docker-compose.dev.yml down                  # Keep data
docker compose -f docker-compose.dev.yml down -v               # Delete data
```

### 🧪 Test API
```bash
curl http://localhost:5000/api/categories                      # Get categories
curl http://localhost:5000/api/products?search=laptop          # Search products
curl http://localhost:5000/health                              # Health check
```

### 🗄️ Database Access
```bash
# Via pgAdmin: http://localhost:5050
# Direct: docker compose -f docker-compose.dev.yml exec db psql -U postgres -d minimalapi_dev
```

## 📝 License

MIT License - Free to use for learning and commercial projects.

---

**Developed with ❤️ using .NET 10 + Angular 19 + PostgreSQL**
