using Consumer;
using Consumer.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

var rabbitMq = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

builder.Services.AddSingleton<ReceivedOrderLog>();
builder.Services.AddSingleton<ReceivedShipmentLog>();
builder.Services.AddSingleton<ReceivedCancellationLog>();

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<OrderSubmittedConsumer>();
    bus.AddConsumer<OrderShippedConsumer>();
    bus.AddConsumer<OrderCancelledConsumer>();
    bus.SetKebabCaseEndpointNameFormatter();

    bus.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMq.Host, rabbitMq.VirtualHost, host =>
        {
            host.Username(rabbitMq.Username);
            host.Password(rabbitMq.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapApi();

app.Run();

namespace Consumer
{
    internal sealed class RabbitMqSettings
    {
        public string Host { get; init; } = "localhost";
        public string VirtualHost { get; init; } = "/";
        public string Username { get; init; } = "lab";
        public string Password { get; init; } = "lab";
    }
}