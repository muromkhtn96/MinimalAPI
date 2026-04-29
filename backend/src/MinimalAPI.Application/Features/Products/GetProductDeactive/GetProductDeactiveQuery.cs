using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductDeactive;

public sealed record GetProductDeactiveQuery() : IRequest<Result<List<ProductDto>>>;