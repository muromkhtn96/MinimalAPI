using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.IntegrationEvents.Orders;

/// <summary>
/// Dòng chi tiết đơn hàng dùng CHUNG cho mọi integration event của Order
/// (OrderCreated, OrderUpdated...) — một schema duy nhất để consumer không phải
/// xử lý mỗi event một kiểu item khác nhau.
/// </summary>
public sealed record OrderEventItem(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// Mapping OrderDetail + Product → OrderEventItem, dùng chung cho CreateOrderHandler
/// và UpdateOrderHandler — logic join details với products chỉ tồn tại MỘT chỗ.
/// </summary>
public static class OrderEventItemMapper
{
    public static IReadOnlyList<OrderEventItem> ToEventItems(
        IReadOnlyList<OrderDetail> details,
        IReadOnlyList<Product> products)
    {
        // Dictionary thay vì First() trong vòng lặp — O(n+m) thay vì O(n*m) với đơn nhiều dòng
        var productsById = products.ToDictionary(product => product.Id);

        return details
            .Select(detail => new OrderEventItem(
                detail.ProductId.Value,
                productsById[detail.ProductId].Name.Value,
                detail.Quantity,
                detail.UnitPrice.Amount))
            .ToList();
    }
}
