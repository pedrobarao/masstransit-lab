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

app.MapApi();

app.Run();

namespace Publisher
{
    internal sealed class RabbitMqSettings
    {
        public string Host { get; init; } = "localhost";
        public string VirtualHost { get; init; } = "/";
        public string Username { get; init; } = "lab";
        public string Password { get; init; } = "lab";
    }
}