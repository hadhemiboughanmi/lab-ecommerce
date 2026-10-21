using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using ECommerce.Ordering.Api.Diagnostics;

namespace ECommerce.Ordering.Tests;

/// <summary>
/// Tests d'intégration de POST /api/orders, avec un faux Catalog (voir <see cref="OrderingApiFactory"/>).
/// </summary>
public class OrderingEndpointsTests(OrderingApiFactory factory) : IClassFixture<OrderingApiFactory>
{
    private const string Customer = "Client Test";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateOrder_SeveralLinesWithQuantities_ReturnsCreatedWithCorrectTotal()
    {
        var request = new CreateOrderRequest(Customer,
        [
            new OrderLine(FakeCatalogHandler.Keyboard.Id, 3),
            new OrderLine(FakeCatalogHandler.Mouse.Id, 4),
        ]);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(3 * 10.00m + 4 * 2.50m, order.Total); // 40.00
        Assert.Equal("Confirmed", order.Status);
        // Nom et prix sont copiés depuis Catalog au moment de la commande.
        var keyboardLine = Assert.Single(order.Items, i => i.ProductId == FakeCatalogHandler.Keyboard.Id);
        Assert.Equal("Fake Keyboard", keyboardLine.ProductName);
        Assert.Equal(10.00m, keyboardLine.UnitPrice);
        Assert.Equal(3, keyboardLine.Quantity);
    }

    [Fact]
    public async Task CreateOrder_EmptyItems_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest(Customer, []);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("An order must contain at least one item.", await response.Content.ReadFromJsonAsync<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CreateOrder_ZeroOrNegativeQuantity_ReturnsBadRequest(int quantity)
    {
        var request = new CreateOrderRequest(Customer, [new OrderLine(FakeCatalogHandler.Keyboard.Id, quantity)]);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_UnknownProduct_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest(Customer, [new OrderLine(999, 1)]);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Product 999 does not exist.", await response.Content.ReadFromJsonAsync<string>());
    }

    [Fact]
    public async Task CreateOrder_Confirmed_IncrementsOrdersCreatedMetric()
    {
        // On écoute la métrique métier comme le ferait un collecteur OpenTelemetry. Les tests
        // d'une même classe s'exécutent en série : seul ce test émet pendant l'écoute.
        long created = 0;
        using var listener = Listen("ecommerce.orders.created",
            (value, _) => Interlocked.Add(ref created, value));
        var request = new CreateOrderRequest(Customer, [new OrderLine(FakeCatalogHandler.Keyboard.Id, 1)]);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, Interlocked.Read(ref created));
    }

    [Fact]
    public async Task CreateOrder_UnknownProduct_IncrementsOrdersRejectedWithReason()
    {
        var reasons = new List<string?>();
        using var listener = Listen("ecommerce.orders.rejected", (_, tags) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "reason") lock (reasons) reasons.Add(tag.Value as string);
            }
        });
        var request = new CreateOrderRequest(Customer, [new OrderLine(999, 1)]);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["unknown_product"], reasons);
    }

    private delegate void Measurement(long value, ReadOnlySpan<KeyValuePair<string, object?>> tags);

    private static MeterListener Listen(string instrumentName, Measurement onMeasurement)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == OrderingTelemetry.Name && instrument.Name == instrumentName)
                    l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => onMeasurement(value, tags));
        listener.Start();
        return listener;
    }

    private sealed record CreateOrderRequest(string Customer, List<OrderLine> Items);
    private sealed record OrderLine(int ProductId, int Quantity);
    private sealed record OrderResponse(int Id, string Customer, DateTimeOffset CreatedAt, string Status, decimal Total, List<OrderItemResponse> Items);
    private sealed record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
}
