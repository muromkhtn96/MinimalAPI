using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public sealed record UpdateProductDeactiveCommand(Guid ProductId)
    : IRequest<Result<ProductDto>>;
