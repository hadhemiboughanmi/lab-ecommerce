using System.Net;
using System.Net.Http.Json;

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
        Assert.Equal(3 * 10.00m, order.Total); // 40.00
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

    private sealed record CreateOrderRequest(string Customer, List<OrderLine> Items);
    private sealed record OrderLine(int ProductId, int Quantity);
    private sealed record OrderResponse(int Id, string Customer, DateTimeOffset CreatedAt, string Status, decimal Total, List<OrderItemResponse> Items);
    private sealed record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
}
