namespace Contracts.Domain.Interfaces
{
    public interface IDateTracking
    {
        DateTimeOffset CreatedAt { get; set; }
        DateTimeOffset LastModifiedAt { get; set; }
    }
}
