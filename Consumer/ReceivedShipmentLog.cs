using System.Collections.Concurrent;
using Contracts;

namespace Consumer;

public sealed class ReceivedShipmentLog
{
    private readonly ConcurrentQueue<OrderShipped> _shipments = new();

    public IReadOnlyList<OrderShipped> All => _shipments.ToArray();

    public void Add(OrderShipped shipment)
    {
        _shipments.Enqueue(shipment);
    }
}