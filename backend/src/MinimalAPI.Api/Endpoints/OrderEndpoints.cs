using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Features.Orders.CancelOrder;
using MinimalAPI.Application.Features.Orders.ConfirmOrder;
using MinimalAPI.Application.Features.Orders.CreateOrder;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Application.Features.Orders.GetOrderById;
using MinimalAPI.Application.Features.Orders.GetOrderByCode;
using MinimalAPI.Application.Features.Orders.GetOrders;
using MinimalAPI.Application.Features.Orders.UpdateOrder;
using MinimalAPI.Application.Features.Orders.GetOrderByCustomerId;

namespace MinimalAPI.Api.Endpoints;

/// <summary>
/// Định nghĩa các endpoint liên quan đến đơn hàng (Order) trong API. 
/// </summary>
public static class OrderEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint liên quan đến đơn hàng vào pipeline của ứng dụng.
    /// </summary>
    /// <param name="app"></param>
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", async Task<IResult> (CreateOrderCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/orders/{result.Value!.Id}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateOrder")
        .WithSummary("Tạo đơn hàng mới")
        .Produces<OrderDto>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        /// <summary>
        /// Lấy danh sách đơn hàng.
        /// </summary>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetOrdersQuery());
            return TypedResults.Ok(result);
        })
        .WithName("GetOrders")
        .WithSummary("Danh sách đơn hàng")
        .Produces<List<OrderDto>>(StatusCodes.Status200OK);
        /// <summary>
        /// Xác nhận đơn hàng.
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapPatch("/{id:guid}/confirm", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ConfirmOrderCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("ConfirmOrder")
        .WithSummary("Xác nhận đơn hàng")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest);
        /// <summary>
        /// Hủy đơn hàng.
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapPatch("/{id:guid}/cancel", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new CancelOrderCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CancelOrder")
        .WithSummary("Hủy đơn hàng")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest);
        /// <summary>
        /// Lấy thông tin chi tiết một đơn hàng theo ID.
        /// </summary>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetOrderById")
        .WithSummary("Xem chi tiết đơn hàng")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound);

        group.MapGet("/by-customer/{customerId:guid}", async Task<IResult> (Guid customerId, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderByCustomerIdQuery(customerId));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetOrderByCustomer")
        .WithSummary("Lấy danh sách đơn hàng theo khách hàng")
        .Produces<List<OrderByCustomerDto>>()
        .Produces(StatusCodes.Status404NotFound);
        /// <summary>
        /// Tra cứu đơn hàng theo mã.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="sender"></param>
        /// <typeparam name="IResult"></typeparam>
        /// <returns></returns>
        group.MapGet("/code/{code}", async Task<IResult> (string code, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderByCodeQuery(code));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetOrderByCode")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status404NotFound)
        .WithSummary("Tra cứu đơn hàng theo mã");

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateOrderCommand command, ISender sender) =>
        {
            if (id != command.OrderId)
                return TypedResults.BadRequest(new { error = "ID không khớp." });
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("UpdateOrder")
        .WithSummary("Cập nhật đơn hàng")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest);
    }
}