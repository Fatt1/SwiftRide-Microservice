using Shared.Enums.Payments;

namespace Trip.API.DTOs;

/// <summary>
/// DTO cho yêu cầu tài xế nhận chuyến đi.
/// </summary>
public sealed record DriverAcceptRequest(Guid DriverId);

/// <summary>
/// DTO cho yêu cầu tài xế từ chối chuyến đi.
/// </summary>
public sealed record DriverRejectRequest(Guid DriverId);

/// <summary>
/// DTO cho yêu cầu tài xế trả khách và kích hoạt thanh toán.
/// </summary>
public sealed record DropOffRequest(
    PaymentMethod PaymentMethod = PaymentMethod.Wallet,
    string? GatewayToken = null
);
