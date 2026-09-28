using Contracts;
using MassTransit;

namespace Consumer.Consumers;

public sealed class OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger, ReceivedOrderLog log)
    : IConsumer<OrderSubmitted>
{
    public Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        log.Add(context.Message);

        logger.LogInformation(
            "Pedido {OrderId} recebido de {Customer} no valor de {Amount}",
            context.Message.OrderId,
            context.Message.Customer,
            context.Message.Amount);

        return Task.CompletedTask;
    }
}