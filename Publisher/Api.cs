using Contracts;
using MassTransit;
using Publisher.Contracts;

namespace Publisher;

public static class Api
{
    public static WebApplication MapApi(this WebApplication app)
    {
        app.MapPost("/orders", async (SubmitOrderRequest request, IPublishEndpoint publishEndpoint) =>
        {
            if (string.IsNullOrWhiteSpace(request.Customer))
                return Results.BadRequest(new { error = "Customer é obrigatório." });

            if (request.Amount <= 0)
                return Results.BadRequest(new { error = "Amount deve ser maior que zero." });

            var message = new OrderSubmitted
            {
                OrderId = Guid.NewGuid(),
                Customer = request.Customer.Trim(),
                Amount = request.Amount,
                SubmittedAt = DateTimeOffset.UtcNow
            };

            await publishEndpoint.Publish(message);

            return Results.Accepted($"/orders/{message.OrderId}", message);
        });

        app.MapPost("/orders/{orderId:guid}/ship",
            async (Guid orderId, ShipOrderRequest request, IPublishEndpoint publishEndpoint) =>
            {
                if (string.IsNullOrWhiteSpace(request.TrackingCode))
                    return Results.BadRequest(new { error = "TrackingCode é obrigatório." });

                var message = new OrderShipped
                {
                    OrderId = orderId,
                    TrackingCode = request.TrackingCode.Trim(),
                    ShippedAt = DateTimeOffset.UtcNow
                };

                await publishEndpoint.Publish(message);

                return Results.Accepted($"/orders/{orderId}", message);
            });

        app.MapPost("/orders/{orderId:guid}/cancel",
            async (Guid orderId, CancelOrderRequest request, IPublishEndpoint publishEndpoint) =>
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                    return Results.BadRequest(new { error = "Reason é obrigatório." });

                var message = new OrderCancelled
                {
                    OrderId = orderId,
                    Reason = request.Reason.Trim(),
                    CancelledAt = DateTimeOffset.UtcNow
                };

                await publishEndpoint.Publish(message);

                return Results.Accepted($"/orders/{orderId}", message);
            });

        return app;
    }
}

internal record SubmitOrderRequest(string Customer, decimal Amount);

internal record ShipOrderRequest(string TrackingCode);

internal record CancelOrderRequest(string Reason);