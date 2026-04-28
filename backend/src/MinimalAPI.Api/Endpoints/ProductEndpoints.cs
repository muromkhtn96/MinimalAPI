using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.CreateProduct;
using MinimalAPI.Application.Features.Products.DeleteProduct;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Application.Features.Products.GetProduct;
using MinimalAPI.Application.Features.Products.GetProductActive;
using MinimalAPI.Application.Features.Products.GetProductsByCategory;
using MinimalAPI.Application.Features.Products.GetProducts;
using MinimalAPI.Application.Features.Products.UpdateProduct;
using MinimalAPI.Application.Features.Products.UpdateProductActive;
using MinimalAPI.Application.Features.Products.GetProductDeactive;
using MinimalAPI.Application.Features.Products.UpdateProductDeactive;

namespace MinimalAPI.Api.Endpoints;

/// <summary>Định nghĩa các endpoint REST cho Product (/api/products).</summary>
public static class ProductEndpoints
{
    /// <summary>Đăng ký tất cả endpoint Product vào pipeline.</summary>
    public static WebApplication MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (
            ISender sender,
            int page = 1,
            int pageSize = 10,
            string? search = null) =>
        {
            var result = await sender.Send(new GetProductsQuery(page, pageSize, search));
            return TypedResults.Ok(result);
        })
        .WithName("GetProducts")
        .WithSummary("Lấy danh sách sản phẩm có phân trang")
        .Produces<PagedResult<ProductDto>>();

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductQuery(id));
            return result is not null
                ? TypedResults.Ok(result)
                : TypedResults.NotFound();
        })
        .WithName("GetProduct")
        .WithSummary("Lấy chi tiết sản phẩm theo Id")
        .Produces<ProductDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async Task<IResult> (CreateProductCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/products/{result.Value}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateProduct")
        .WithSummary("Tạo sản phẩm mới")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateProductCommand command, ISender sender) =>
        {
            if (id != command.Id)
                return TypedResults.BadRequest(new { error = "Id không khớp." });

            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("UpdateProduct")
        .WithSummary("Cập nhật sản phẩm")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeleteProduct")
        .WithSummary("Xóa sản phẩm")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/by-category/{categoryId:guid}", async Task<IResult> (Guid categoryId, ISender sender) =>
        {
            var result = await sender.Send(new GetProductsByCategoryQuery(categoryId));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetProductsByCategory")
        .WithSummary("Lấy danh sách sản phẩm theo danh mục")
        .Produces<List<ProductDto>>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/active", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetProductActiveQuery());
            return TypedResults.Ok(result.Value);
        })
        .WithName("GetProductActive")
        .WithSummary("Lấy danh sách sản phẩm đang hoạt động")
        .Produces<List<ProductDto>>();

        group.MapGet("/deactive", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetProductDeactiveQuery());
            return TypedResults.Ok(result.Value);
        })
        .WithName("GetProductDeactive")
        .WithSummary("Lấy danh sách sản phẩm không hoạt động")
        .Produces<List<ProductDto>>();

        group.MapPut("/{id:guid}/active", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProductActiveCommand(id, true));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("ActivateProduct")
        .WithSummary("Bật trạng thái hoạt động của sản phẩm")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/deactive", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProductDeactiveCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeactivateProduct")
        .WithSummary("Tắt trạng thái hoạt động của sản phẩm")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
