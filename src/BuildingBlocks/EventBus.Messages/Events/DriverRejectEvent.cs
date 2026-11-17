using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

/// <summary>
/// Published when a driver rejects a trip proposal.
/// </summary>
public record DriverRejectEvent : IntegrationBaseEvent
{
    public Guid TripId { get; init; }
    public string MatchSessionId { get; init; } = "";

    public Guid DriverId { get; init; }
    public Guid? RiderId { get; init; }
    public int RejectCount { get; init; } = 1;
    // Trip quyết định chuyến đã thất bại do vượt k; Matching chỉ cập nhật theo kết quả này.
    public bool TripFailed { get; init; }
    public string Reason { get; init; } = "Tài xế đã từ chối chuyến đi";
}
