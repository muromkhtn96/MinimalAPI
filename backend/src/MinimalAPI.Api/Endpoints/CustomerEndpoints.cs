using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Features.Customers.CreateCustomer;
using MinimalAPI.Application.Features.Customers.GetCustomers;
using MinimalAPI.Application.Features.Customers.GetCustomer;
using MinimalAPI.Application.Features.Customers.GetCustomerByCode;
using MinimalAPI.Application.Features.Customers.UpdateCustomer;
using MinimalAPI.Application.Features.Customers.ActivateCustomer;
using MinimalAPI.Application.Features.Customers.DeactivateCustomer;
using MinimalAPI.Application.Features.Customers.DeleteCustomer;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Api.Endpoints;
public static class CustomerEndpoints
{
    public static WebApplication MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers");

        group.MapPost("/", async Task<IResult> (CreateCustomerCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/customers/{result.Value!.Id}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateCustomer")
        .WithSummary("Tạo khách hàng")
        .Produces<CustomerDto>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/", async Task<IResult> (ISender sender) =>
        {
            var result = await sender.Send(new GetCustomersQuery());
            return TypedResults.Ok(result); 
        })
        .WithName("GetCustomers")
        .WithSummary("Danh sách khách hàng")
        .Produces<List<CustomerDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetCustomerByIdQuery(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetCustomer")
        .WithSummary("Chi tiết khách hàng")
        .Produces<CustomerDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/code/{code}", async Task<IResult> (string code, ISender sender) =>
        {
            var result = await sender.Send(new GetCustomerByCodeQuery(code));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("GetCustomerByCode")
        .WithSummary("Tra cứu theo mã")
        .Produces<CustomerDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateCustomerCommand command, ISender sender) =>
        {
            if (id != command.Id)
                return TypedResults.BadRequest(new { error = "Id không khớp." });

            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("UpdateCustomer")
        .WithSummary("Cập nhật thông tin")
        .Produces<CustomerDto>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPatch("/{id:guid}/active", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ActivateCustomerCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("ActivateCustomer")
        .WithSummary("Kích hoạt")
        .Produces<bool>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/deactive", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateCustomerCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeactivateCustomer")
        .WithSummary("Vô hiệu hóa")
        .Produces<bool>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCustomerCommand(id));
            return result.IsSuccess
                ? TypedResults.NoContent()
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeleteCustomer")
        .WithSummary("Xóa")
        .Produces<CustomerDto>() 
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}       