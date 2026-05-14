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

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/inventories")
            .WithTags("Inventories");

        group.MapGet("/", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetInventoriesQuery());
            return TypedResults.Ok(result);
        })
        .WithName("GetInventories")
        .Produces<List<InventoryDto>>(StatusCodes.Status200OK)
        .WithSummary("Lấy danh sách tồn kho");

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

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetInventoryQuery(new InventoryId(id)));
            return result.Any() ? TypedResults.Ok(result.First()) : TypedResults.NotFound();
        })
        .WithName("GetInventoryById")
        .Produces<InventoryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithSummary("Lấy tồn kho theo id");

        group.MapGet("/by-product/{productId:guid}", async Task<IResult> (Guid productId, ISender sender) =>
        {
            var result = await sender.Send(new GetInventoryByProductQuery(new ProductId(productId)));
            return result.Any() ? TypedResults.Ok(result.First()) : TypedResults.NotFound();
        })
        .WithName("GetInventoryByProduct")
        .Produces<InventoryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithSummary("Lấy tồn kho theo productid");   

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
