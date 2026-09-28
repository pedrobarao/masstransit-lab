namespace Publisher;

public record OrderSubmitted
{
    public Guid OrderId { get; init; }
    public string Customer { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
}
