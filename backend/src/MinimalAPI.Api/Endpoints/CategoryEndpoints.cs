using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.CreateCategory;
using MinimalAPI.Application.Features.Categories.DeleteCategory;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Application.Features.Categories.GetCategories;
using MinimalAPI.Application.Features.Categories.GetCategory;
using MinimalAPI.Application.Features.Categories.UpdateCategory;

namespace MinimalAPI.Api.Endpoints;

/// <summary>Định nghĩa các endpoint REST cho Category (/api/categories).</summary>
public static class CategoryEndpoints
{
    /// <summary>Đăng ký tất cả endpoint Category vào pipeline.</summary>
    public static WebApplication MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories");

        group.MapGet("/", async (ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetCategoriesQuery(page, pageSize));
            return TypedResults.Ok(result);
        })
        .WithName("GetCategories")
        .WithSummary("Lấy danh sách danh mục")
        .Produces<PagedResult<CategoryDto>>();

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetCategoryQuery(id));
            return result is not null
                ? TypedResults.Ok(result)
                : TypedResults.NotFound();
        })
        .WithName("GetCategory")
        .WithSummary("Lấy chi tiết danh mục theo Id")
        .Produces<CategoryDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async Task<IResult> (CreateCategoryCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/categories/{result.Value}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateCategory")
        .WithSummary("Tạo danh mục mới")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateCategoryCommand command, ISender sender) =>
        {
            if (id != command.Id)
                return TypedResults.BadRequest(new { error = "Id không khớp." });

            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("UpdateCategory")
        .WithSummary("Cập nhật danh mục")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeleteCategory")
        .WithSummary("Xóa danh mục")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
