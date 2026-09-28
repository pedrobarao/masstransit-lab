namespace Contracts;

public record OrderShipped
{
    public Guid OrderId { get; init; }
    public string TrackingCode { get; init; } = string.Empty;
    public DateTimeOffset ShippedAt { get; init; }
}