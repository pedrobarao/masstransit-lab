using MassTransit;
using Publisher;

var builder = WebApplication.CreateBuilder(args);

var rabbitMq = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

builder.Services.AddMassTransit(bus =>
{
    bus.UsingRabbitMq((_, cfg) =>
    {
        cfg.Host(rabbitMq.Host, rabbitMq.VirtualHost, host =>
        {
            host.Username(rabbitMq.Username);
            host.Password(rabbitMq.Password);
        });
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

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

app.Run();

namespace Publisher
{
    record SubmitOrderRequest(string Customer, decimal Amount);

    sealed class RabbitMqSettings
    {
        public string Host { get; init; } = "localhost";
        public string VirtualHost { get; init; } = "/";
        public string Username { get; init; } = "lab";
        public string Password { get; init; } = "lab";
    }
}