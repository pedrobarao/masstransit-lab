using System.Collections.Concurrent;
using Consumer.Contracts;

namespace Consumer;

public sealed class ReceivedCancellationLog
{
    private readonly ConcurrentQueue<OrderCancelled> _cancellations = new();

    public IReadOnlyList<OrderCancelled> All => _cancellations.ToArray();

    public void Add(OrderCancelled cancellation) => _cancellations.Enqueue(cancellation);
}
