using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductsByCategory;

public sealed record GetProductsByCategoryQuery(Guid CategoryId) : IRequest<Result<List<ProductDto>>>;