using Consumer.Contracts;
using MassTransit;

namespace Consumer.Consumers;

public sealed class OrderCancelledConsumer(ILogger<OrderCancelledConsumer> logger, Consumer.ReceivedCancellationLog log)
    : IConsumer<OrderCancelled>
{
    public Task Consume(ConsumeContext<OrderCancelled> context)
    {
        log.Add(context.Message);

        logger.LogInformation(
            "Pedido {OrderId} cancelado: {Reason}",
            context.Message.OrderId,
            context.Message.Reason);

        return Task.CompletedTask;
    }
}
