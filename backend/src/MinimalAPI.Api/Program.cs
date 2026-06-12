using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Api.Endpoints;
using MinimalAPI.Application;
using MinimalAPI.Domain.Exceptions;
using MinimalAPI.Infrastructure;
using MinimalAPI.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — đọc config từ appsettings
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Application + Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Health checks — DB connectivity
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// Rate limiting — chống spam/DDoS
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ProblemDetails + Swagger
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MinimalAPI", Version = "v1" });

    // Tích hợp XML comments nếu file tồn tại
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, "MinimalAPI.Api.xml");
    if (File.Exists(apiXmlPath))
        c.IncludeXmlComments(apiXmlPath);

    var appXmlPath = Path.Combine(AppContext.BaseDirectory, "MinimalAPI.Application.xml");
    if (File.Exists(appXmlPath))
        c.IncludeXmlComments(appXmlPath);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:8080", "http://localhost")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration.GetValue<string>("AllowedOrigins") ?? "";
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Global exception handler — trả ProblemDetails chuẩn RFC 9457
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        // LƯU Ý: switch theo thứ tự từ loại cụ thể (dẫn xuất) -> tổng quát.
        // DbUpdateConcurrencyException kế thừa DbUpdateException nên phải đứng trước;
        // TaskCanceledException kế thừa OperationCanceledException nên được bao luôn.
        var problemDetails = exception switch
        {
            ValidationException validationEx           => BuildValidationProblem(validationEx, context, logger),
            BadHttpRequestException badReqEx           => BuildBadRequestProblem(badReqEx, context, logger),
            DomainException domainEx                   => BuildDomainProblem(domainEx, context, logger),
            OperationCanceledException                 => BuildClientClosedProblem(context, logger),
            DbUpdateConcurrencyException concurrencyEx => BuildConcurrencyProblem(concurrencyEx, context, logger),
            DbUpdateException dbUpdateEx               => BuildConflictProblem(dbUpdateEx, context, logger),
            UnauthorizedAccessException unauthorizedEx => BuildForbiddenProblem(unauthorizedEx, context, logger),
            KeyNotFoundException notFoundEx            => BuildNotFoundProblem(notFoundEx, context, logger),
            _                                          => BuildUnhandledProblem(exception, context, logger)
        };

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("frontend");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapInventoryEndpoints();
app.MapCustomerEndpoints();
app.MapOrderEndpoints();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await db.Database.MigrateAsync();
        startupLogger.LogInformation("Database migration applied successfully");

        if (app.Environment.IsDevelopment())
        {
            await SeedData.SeedAsync(db);
            startupLogger.LogInformation("Seed data applied (Development only)");
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Database migration failed — application cannot start");
        throw;
    }
}

app.Run();

/// -------------------Helpers------------------- ///
static Microsoft.AspNetCore.Mvc.ProblemDetails BuildValidationProblem(
    ValidationException ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning(ex, "Xác thực thất bại tại {Path}", context.Request.Path);

    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Lỗi xác thực dữ liệu",
        Detail = "Dữ liệu gửi lên không hợp lệ, vui lòng kiểm tra lại.",
        Instance = context.Request.Path
    };

    problem.Extensions["errors"] = ex.Errors
        .GroupBy(e => e.PropertyName)
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

    return problem;
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildDomainProblem(
    DomainException ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning(ex, "Vi phạm quy tắc nghiệp vụ: {Message}", ex.Message);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status422UnprocessableEntity,
        Title = "Vi phạm quy tắc nghiệp vụ",
        Detail = ex.Message, // Message từ Domain thường đã được viết bằng tiếng Việt
        Instance = context.Request.Path
    };
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildBadRequestProblem(
    BadHttpRequestException ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning(ex, "Yêu cầu không hợp lệ tại {Path}", context.Request.Path);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Yêu cầu không hợp lệ",
        Detail = "Nội dung gửi lên không đọc được — JSON sai cú pháp hoặc sai mã hoá (phải là UTF-8).",
        Instance = context.Request.Path
    };
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildClientClosedProblem(
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning("Yêu cầu bị hủy bởi client tại {Path}", context.Request.Path);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = 499, // Client Closed Request (quy ước nginx — client ngắt kết nối trước khi xử lý xong)
        Title = "Yêu cầu đã bị hủy",
        Detail = "Yêu cầu bị hủy hoặc client ngắt kết nối trước khi xử lý hoàn tất.",
        Instance = context.Request.Path
    };
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildConcurrencyProblem(
    DbUpdateConcurrencyException ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning(ex, "Xung đột đồng thời tại {Path}", context.Request.Path);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Xung đột dữ liệu",
        Detail = "Dữ liệu đã bị thay đổi bởi thao tác khác. Vui lòng tải lại và thử lại.",
        Instance = context.Request.Path
    };
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildConflictProblem(
    DbUpdateException ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning(ex, "Vi phạm ràng buộc dữ liệu tại {Path}", context.Request.Path);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Vi phạm ràng buộc dữ liệu",
        Detail = "Không thể lưu — dữ liệu có thể bị trùng hoặc đang được tham chiếu bởi bản ghi khác.",
        Instance = context.Request.Path
    };
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildForbiddenProblem(
    UnauthorizedAccessException ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning(ex, "Truy cập bị từ chối tại {Path}", context.Request.Path);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Không có quyền truy cập",
        Detail = "Bạn không có quyền thực hiện thao tác này.",
        Instance = context.Request.Path
    };
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildNotFoundProblem(
    KeyNotFoundException ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogWarning(ex, "Không tìm thấy tài nguyên tại {Path}", context.Request.Path);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Không tìm thấy",
        Detail = "Không tìm thấy tài nguyên được yêu cầu.",
        Instance = context.Request.Path
    };
}

static Microsoft.AspNetCore.Mvc.ProblemDetails BuildUnhandledProblem(
    Exception? ex,
    HttpContext context,
    Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogError(ex, "Lỗi không xác định tại {Path}", context.Request.Path);

    return new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Lỗi hệ thống",
        Detail = "Đã có lỗi bất ngờ xảy ra. Vui lòng thử lại sau hoặc liên hệ quản trị viên.",
        Instance = context.Request.Path
    };
}
