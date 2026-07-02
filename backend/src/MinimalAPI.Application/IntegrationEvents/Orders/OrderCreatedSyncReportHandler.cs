using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions.Messaging;

namespace MinimalAPI.Application.IntegrationEvents.Orders;

/// <summary>
/// Handler của consumer group "minimalapi.orders.sync-report" — mục đích: ĐỒNG BỘ sang hệ thống báo cáo.
/// Chạy trên group RIÊNG, offset riêng — độc lập hoàn toàn với group send-email dù đọc cùng topic:
/// email chậm/chết không làm trễ báo cáo. Thêm nhu cầu mới = thêm handler + thêm group vào appsettings,
/// KHÔNG sửa producer, KHÔNG sửa KafkaConsumerService.
/// Demo: log thay cho ghi vào warehouse/report DB thật.
/// </summary>
public sealed class OrderCreatedSyncReportHandler(
    ILogger<OrderCreatedSyncReportHandler> logger)
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public Task HandleAsync(OrderCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[SyncReport] Ghi nhận đơn {Code} vào báo cáo doanh thu ngày {Date:yyyy-MM-dd} — {TotalAmount} {Currency}",
            integrationEvent.Code,
            integrationEvent.CreatedAt,
            integrationEvent.TotalAmount,
            integrationEvent.Currency);

        // Nghiệp vụ thật: upsert theo OrderId (tự nhiên idempotent) thay vì insert
        return Task.CompletedTask;
    }
}
