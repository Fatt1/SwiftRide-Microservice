using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

/// <summary>
/// Sự kiện được xuất bản bởi Identity Service khi người dùng đăng ký tài khoản thành công.
/// </summary>
public record UserCreatedEvent : IntegrationBaseEvent
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
}

