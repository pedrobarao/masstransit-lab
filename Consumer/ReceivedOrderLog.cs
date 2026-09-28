using System.Collections.Concurrent;

namespace Consumer;

public sealed class ReceivedOrderLog
{
    private readonly ConcurrentQueue<OrderSubmitted> _orders = new();

    public void Add(OrderSubmitted order) => _orders.Enqueue(order);

    public IReadOnlyList<OrderSubmitted> All => _orders.ToArray();
}
