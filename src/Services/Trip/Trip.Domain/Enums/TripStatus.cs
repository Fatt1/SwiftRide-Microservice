namespace Trip.Domain.Enums;

public enum TripStatus
{
    Requested = 0,
    DriverAccepted = 1,
    PickedUp = 3,
    DroppedOff = 4,
    PaymentPending = 5,
    Completed = 6,
    Cancelled = 7,
    Failed = 8
}
