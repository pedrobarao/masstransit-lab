using MassTransit;

namespace Consumer.Contracts;

[EntityName("Contracts:OrderCancelled")]
[MessageUrn("Contracts:OrderCancelled")]
public record OrderCancelled
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset CancelledAt { get; init; }
}
