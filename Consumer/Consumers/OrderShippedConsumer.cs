using Contracts;
using MassTransit;

namespace Consumer.Consumers;

public sealed class OrderShippedConsumer(ILogger<OrderShippedConsumer> logger, ReceivedShipmentLog log)
    : IConsumer<OrderShipped>
{
    public Task Consume(ConsumeContext<OrderShipped> context)
    {
        log.Add(context.Message);

        logger.LogInformation(
            "Pedido {OrderId} enviado com rastreio {TrackingCode}",
            context.Message.OrderId,
            context.Message.TrackingCode);

        return Task.CompletedTask;
    }
}