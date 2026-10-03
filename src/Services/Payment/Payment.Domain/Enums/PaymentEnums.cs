namespace Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Completed,
    Refunded,
    Failed
}

public enum PaymentMethod
{
    Wallet,
    Card
}

public enum EntryType
{
    Debit,
    Credit
}

public enum RefundStatus
{
    Pending,
    Completed,
    Failed
}
