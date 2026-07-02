using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions.Messaging;

namespace MinimalAPI.Application.IntegrationEvents.Orders;

/// <summary>
/// Handler của consumer group "minimalapi.orders.send-email" — mục đích: GỬI EMAIL xác nhận đơn hàng.
/// Topic minimalapi.orders có NHIỀU consumer group cùng đọc độc lập (xem Kafka:Consumers trong appsettings),
/// group này chỉ quan tâm OrderCreated — các event khác trong topic được bỏ qua.
/// Demo: log thay cho tích hợp email service thật (SendGrid, SES...).
/// </summary>
public sealed class OrderCreatedSendEmailHandler(
    ILogger<OrderCreatedSendEmailHandler> logger)
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public Task HandleAsync(OrderCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[SendEmail] Gửi email xác nhận đơn {Code} cho khách {CustomerId} — {ItemCount} sản phẩm, tổng tiền {TotalAmount} {Currency}",
            integrationEvent.Code,
            integrationEvent.CustomerId,
            integrationEvent.Items.Count,
            integrationEvent.TotalAmount,
            integrationEvent.Currency);

        // Nghiệp vụ thật đặt ở đây — phải idempotent: at-least-once có thể giao event 2 lần,
        // khách không được nhận 2 email (check EventId đã xử lý chưa trước khi gửi)
        return Task.CompletedTask;
    }
}
