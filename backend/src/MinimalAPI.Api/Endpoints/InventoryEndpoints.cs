using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Application.Features.Inventories.CreateInventory;
using MinimalAPI.Application.Features.Inventories.UpdateInventory;
using MinimalAPI.Application.Features.Inventories.DeleteInventory;
using MinimalAPI.Application.Features.Inventories.GetInventory;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Application.Features.Inventories.GetInventories;
using MinimalAPI.Application.Features.Inventories.GetInventoryByProduct;

namespace MinimalAPI.Api.Endpoints;

/// <summary>Định nghĩa các endpoint REST cho Inventory (/api/inventories).</summary>
public static class InventoryEndpoints
{
    /// <summary>Đăng ký tất cả endpoint Inventory vào pipeline.</summary>
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        /// <summary>
        /// Định nghĩa các endpoint REST cho Inventory (/api/inventories).
        /// </summary>
        /// <returns></returns>
        var group = app.MapGroup("/api/inventories")
            .WithTags("Inventories");
        /// <summary>
        /// Lấy danh sách tồn kho
        /// </summary>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetInventoriesQuery());
            return TypedResults.Ok(result);
        })
        .WithName("GetInventories")
        .Produces<List<InventoryDto>>(StatusCodes.Status200OK)
        .WithSummary("Lấy danh sách tồn kho");
        /// <summary>
        /// Tạo tồn kho mới
        /// </summary>
        /// <param name="command"></param>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapPost("/", async Task<IResult> (CreateInventoryCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/inventories/{result.Value!.ProductId}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateInventory")
        .Produces<InventoryDto>(StatusCodes.Status201Created)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .WithSummary("Tạo tồn kho mới");
        /// <summary>
        /// Cập nhật tồn kho
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateInventoryCommand command, ISender sender) =>
        {
            var updatedCommand = command with { Id = id };

            var result = await sender.Send(updatedCommand);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("UpdateInventory")
        .Produces<InventoryDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .WithSummary("Cập nhật tồn kho");
        /// <summary>
        /// Lấy tồn kho theo id
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetInventoryQuery(new InventoryId(id)));
            return result.Any() ? TypedResults.Ok(result.First()) : TypedResults.NotFound();
        })
        .WithName("GetInventoryById")
        .Produces<InventoryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithSummary("Lấy tồn kho theo id");
        /// <summary>
        /// Lấy tồn kho theo product id
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/by-product/{productId:guid}", async Task<IResult> (Guid productId, ISender sender) =>
        {
            var result = await sender.Send(new GetInventoryByProductQuery(new ProductId(productId)));
            return result.Any() ? TypedResults.Ok(result.First()) : TypedResults.NotFound();
        })
        .WithName("GetInventoryByProduct")
        .Produces<InventoryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithSummary("Lấy tồn kho theo productid");   
        /// <summary>
        /// Xóa tồn kho
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteInventoryCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("DeleteInventory")
        .Produces<InventoryDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .WithSummary("Xóa tồn kho");
    }
}
