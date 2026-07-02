using Microsoft.Extensions.Logging;

namespace MinimalAPI.Application.Abstractions.Messaging;

/// <summary>
/// Chính sách publish SAU COMMIT dùng chung cho mọi handler:
///   - Nghiệp vụ ĐÃ lưu DB thành công → lỗi publish chỉ được log, KHÔNG được làm fail request.
///   - Không truyền CancellationToken của request: client ngắt kết nối sau commit
///     không được làm rơi event (dữ liệu đã đổi thì event phải được bắn).
/// Phase 6 roadmap: thay bằng Outbox pattern để không mất event khi broker down.
/// </summary>
public static class IntegrationEventPublisherExtensions
{
    public static async Task PublishAfterCommitAsync<TEvent>(
        this IIntegrationEventPublisher publisher,
        TEvent integrationEvent,
        ILogger logger)
        where TEvent : IntegrationEvent
    {
        try
        {
            // CancellationToken.None có chủ đích — xem ghi chú ở đầu file
            await publisher.PublishAsync(integrationEvent, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Publish {EventType} (EventId {EventId}, Key {PartitionKey}) thất bại — nghiệp vụ ĐÃ hoàn tất, chỉ mất event (Phase 6 Outbox sẽ xử lý triệt để)",
                integrationEvent.GetType().Name, integrationEvent.EventId, integrationEvent.PartitionKey);
        }
    }
}
