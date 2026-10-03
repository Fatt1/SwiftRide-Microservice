namespace Trip.Domain.Enums;

public enum TripStatus
{
    Requested,
    DriverSearching,
    DriverAccepted,
    PickedUp,
    DroppedOff,
    PaymentPending,
    Paid,
    Cancelled
}
