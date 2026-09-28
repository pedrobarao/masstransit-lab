namespace Consumer;

public static class Api
{
    public static WebApplication MapApi(this WebApplication app)
    {
        app.MapGet("/orders", (ReceivedOrderLog log) => log.All);
        app.MapGet("/shipments", (ReceivedShipmentLog log) => log.All);
        app.MapGet("/cancellations", (ReceivedCancellationLog log) => log.All);
        return app;
    }
}