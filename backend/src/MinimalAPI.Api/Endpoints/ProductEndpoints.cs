using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.CreateProduct;
using MinimalAPI.Application.Features.Products.DeleteProduct;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Application.Features.Products.GetProduct;
using MinimalAPI.Application.Features.Products.GetProductDeactive;
using MinimalAPI.Application.Features.Products.GetProducts;
using MinimalAPI.Application.Features.Products.UpdateProduct;
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

        group.MapGet("/deactive", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetProductDeactiveQuery());
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("GetProductDeactive")
        .WithSummary("Lấy danh sách sản phẩm đã tắt hoạt động")
        .Produces<List<ProductDto>>();

        group.MapPut("/deactive/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProductDeactiveCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("UpdateProductDeactive")
        .WithSummary("Tắt hoạt động sản phẩm");
        
        return app;
    }
}
