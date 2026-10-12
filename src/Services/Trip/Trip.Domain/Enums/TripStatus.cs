namespace Trip.Domain.Enums;

public enum TripStatus
{
    Requested,
    DriverAccepted,
    DriverRejected,
    PickedUp,
    DroppedOff,
    Completed,
    PaymentFailed,
    Cancelled
}
