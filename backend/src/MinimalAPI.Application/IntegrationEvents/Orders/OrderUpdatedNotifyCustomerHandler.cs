using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions.Messaging;

namespace MinimalAPI.Application.IntegrationEvents.Orders;

/// <summary>
/// Handler của consumer group "minimalapi.orders.notify-customer" — mục đích: THÔNG BÁO khách hàng
/// đơn đã thay đổi. Group này chỉ quan tâm OrderUpdated trong topic minimalapi.orders.
/// Demo: log thay cho bắn push notification / SMS thật.
/// </summary>
public sealed class OrderUpdatedNotifyCustomerHandler(
    ILogger<OrderUpdatedNotifyCustomerHandler> logger)
    : IIntegrationEventHandler<OrderUpdatedIntegrationEvent>
{
    public Task HandleAsync(OrderUpdatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[NotifyCustomer] Thông báo khách {CustomerId}: đơn {Code} đã cập nhật lúc {UpdatedAt:HH:mm:ss dd/MM/yyyy} — tổng tiền mới {TotalAmount} {Currency}, {ItemCount} sản phẩm",
            integrationEvent.CustomerId,
            integrationEvent.Code,
            integrationEvent.UpdatedAt,
            integrationEvent.TotalAmount,
            integrationEvent.Currency,
            integrationEvent.Items.Count);

        return Task.CompletedTask;
    }
}
