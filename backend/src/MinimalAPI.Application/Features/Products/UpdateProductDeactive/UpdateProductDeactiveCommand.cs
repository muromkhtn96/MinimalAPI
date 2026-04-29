using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public sealed record UpdateProductDeactiveCommand(Guid ProductId)
    : IRequest<Result<Guid>>;
