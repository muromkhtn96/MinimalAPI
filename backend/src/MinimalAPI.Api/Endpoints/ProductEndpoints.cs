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
using MinimalAPI.Application.Features.Products.GetProductByCode;

namespace MinimalAPI.Api.Endpoints;

/// <summary>Định nghĩa các endpoint REST cho Product (/api/products).</summary>
public static class ProductEndpoints
{
    /// <summary>Đăng ký tất cả endpoint Product vào pipeline.</summary>
    public static WebApplication MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");
        /// <summary>
        /// Lấy danh sách sản phẩm có phân trang và tìm kiếm theo tên (query string: ?page=1&pageSize=10&search=keyword)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="search"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Lấy chi tiết sản phẩm theo Id
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductByIdQuery(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetProduct")
        .WithSummary("Lấy chi tiết sản phẩm theo Id")
        .Produces<ProductDto>()
        .Produces(StatusCodes.Status404NotFound);
        /// <summary>
        /// Tạo sản phẩm mới
        /// </summary>
        /// <param name="command"></param>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapPost("/", async Task<IResult> (CreateProductCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/products/{result.Value!.Id}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateProduct")
        .WithSummary("Tạo sản phẩm mới")
        .Produces<ProductDto>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
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
        .Produces<ProductDto>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeleteProduct")
        .WithSummary("Xóa sản phẩm")
        .Produces<ProductDto>()
        .Produces(StatusCodes.Status404NotFound);
        /// <summary>
        /// Lấy danh sách sản phẩm theo danh mục
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/by-category/{categoryId:guid}", async Task<IResult> (Guid categoryId, ISender sender) =>
        {
            var result = await sender.Send(new GetProductsByCategoryIdQuery(categoryId));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetProductsByCategory")
        .WithSummary("Lấy danh sách sản phẩm theo danh mục")
        .Produces<List<ProductDto>>()
        .Produces(StatusCodes.Status404NotFound);
        /// <summary>
        /// Lấy sản phẩm theo mã code
        /// </summary>
        /// <param name="code"></param>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/code/{code}", async Task<IResult> (string code, ISender sender) =>
        {
            var result = await sender.Send(new GetProductByCodeQuery(code));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
            
        })
        .WithName("GetProductByCode")
        .WithSummary("Lấy danh sách sản phảm theo mã code")
        .Produces<List<ProductDto>>()
        .Produces(StatusCodes.Status404NotFound);
        /// <summary>
        /// Lấy danh sách sản phẩm đang hoạt động
        /// </summary>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/active", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetProductActiveQuery());
            return TypedResults.Ok(result.Value);
        })
        .WithName("GetProductActive")
        .WithSummary("Lấy danh sách sản phẩm đang hoạt động")
        .Produces<List<ProductDto>>();
        /// <summary>
        /// Lấy danh sách sản phẩm không hoạt động
        /// </summary>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/deactive", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetProductDeactiveQuery());
            return TypedResults.Ok(result.Value);
        })
        .WithName("GetProductDeactive")
        .WithSummary("Lấy danh sách sản phẩm không hoạt động")
        .Produces<List<ProductDto>>();
        /// <summary>
        /// Cập nhật trạng thái hoạt động của sản phẩm
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapPut("/{id:guid}/active", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProductActiveCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("ActivateProduct")
        .WithSummary("Bật trạng thái hoạt động của sản phẩm")
        .Produces<ProductDto>()
        .Produces(StatusCodes.Status404NotFound);
        /// <summary>
        /// Tắt trạng thái hoạt động của sản phẩm
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapPut("/{id:guid}/deactive", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProductDeactiveCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeactivateProduct")
        .WithSummary("Tắt trạng thái hoạt động của sản phẩm")
        .Produces<ProductDto>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
