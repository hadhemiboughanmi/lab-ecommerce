using System.Net;
using System.Net.Http.Json;
using ECommerce.Ordering.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Ordering.Tests;

/// <summary>
/// Démarre l'API Ordering en mémoire, sans le service Catalog : le client typé
/// <see cref="CatalogServiceClient"/> reçoit un faux handler qui répond à sa place.
/// </summary>
public class OrderingApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Le service discovery doit pouvoir résoudre "catalog" avant d'atteindre le faux handler.
        builder.UseSetting("services:catalog:http:0", "http://catalog.test");

        builder.ConfigureTestServices(services =>
            services.AddHttpClient<CatalogServiceClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new FakeCatalogHandler()));
    }
}

/// <summary>
/// Faux catalogue : connaît deux produits fictifs, répond 404 pour tout autre id.
/// </summary>
public class FakeCatalogHandler : HttpMessageHandler
{
    public static readonly CatalogProduct Keyboard = new(1, "Fake Keyboard", null, 10.00m, 100);
    public static readonly CatalogProduct Mouse = new(2, "Fake Mouse", null, 2.50m, 100);

    private static readonly Dictionary<int, CatalogProduct> Products = new()
    {
        [Keyboard.Id] = Keyboard,
        [Mouse.Id] = Mouse,
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var segments = request.RequestUri!.AbsolutePath.TrimEnd('/').Split('/');
        if (segments is [.., "api", "products", var rawId]
            && int.TryParse(rawId, out var id)
            && Products.TryGetValue(id, out var product))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(product)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
