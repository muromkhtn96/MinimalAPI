using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductActive;

public sealed record GetProductActiveQuery() : IRequest<Result<List<ProductDto>>>;