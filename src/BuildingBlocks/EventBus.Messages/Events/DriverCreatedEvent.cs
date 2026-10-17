using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

/// <summary>
/// Sự kiện được xuất bản bởi Identity Service khi tài xế đăng ký tài khoản thành công.
/// </summary>
public record DriverCreatedEvent : IntegrationBaseEvent
{
    public Guid DriverId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
}

public record DriverCreated : DriverCreatedEvent
{
}
