using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public sealed record UpdateProductActiveCommand(Guid Id) : IRequest<Result<ProductDto>>;
