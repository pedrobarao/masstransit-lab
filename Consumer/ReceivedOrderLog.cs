using System.Collections.Concurrent;
using Contracts;

namespace Consumer;

public sealed class ReceivedOrderLog
{
    private readonly ConcurrentQueue<OrderSubmitted> _orders = new();

    public IReadOnlyList<OrderSubmitted> All => _orders.ToArray();

    public void Add(OrderSubmitted order)
    {
        _orders.Enqueue(order);
    }
}