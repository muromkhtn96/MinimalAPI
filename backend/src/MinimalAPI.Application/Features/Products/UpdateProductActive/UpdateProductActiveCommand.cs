using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public sealed record UpdateProductActiveCommand(Guid Id, bool IsActive) :
 IRequest<Result<Guid>>;
