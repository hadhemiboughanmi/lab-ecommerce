using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Catalog.Tests;

/// <summary>
/// Tests d'intégration des endpoints du catalogue : l'API tourne en mémoire
/// (WebApplicationFactory), sur un vrai PostgreSQL migré avec son seed (voir <see cref="CatalogApiFactory"/>).
/// </summary>
public class CatalogEndpointsTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetProducts_SeededCatalog_ReturnsOkWithSeedProducts()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        // D'autres tests peuvent créer des produits : on vérifie la présence des 4 d'amorçage,
        // pas un nombre exact.
        Assert.Superset(new HashSet<int> { 1, 2, 3, 4 }, products.Select(p => p.Id).ToHashSet());
    }

    [Fact]
    public async Task GetProductById_ExistingId_ReturnsProduct()
    {
        var response = await _client.GetAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(119.99m, product.Price);
    }

    [Fact]
    public async Task GetProductById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_NonIntegerId_ReturnsNotFound()
    {
        // La contrainte de route {id:int} ne correspond pas : aucune route, donc 404 (et non 400).
        var response = await _client.GetAsync("/api/products/abc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreatedAndProductIsReadable()
    {
        var request = new
        {
            Sku = UniqueSku(),
            Name = $"Test Product {Guid.NewGuid():N}",
            Description = "Produit fictif de test",
            Price = 12.34m,
            AvailableStock = 7,
            CategoryId = AccessoriesCategoryId
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(created);
        Assert.DoesNotContain(created.Id, new[] { 1, 2, 3, 4 }); // pas de collision avec l'amorçage
        Assert.NotNull(createResponse.Headers.Location);

        var readResponse = await _client.GetAsync(createResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var read = await readResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(created, read);
        Assert.Equal(request.Name, read!.Name);
        Assert.Equal(request.Price, read.Price);
        Assert.Equal(request.AvailableStock, read.AvailableStock);
        Assert.Equal(request.Sku, read.Sku);
        Assert.Equal(request.CategoryId, read.CategoryId);
    }

    [Fact]
    public async Task CreateProduct_DuplicateSku_ReturnsConflict()
    {
        // KB-MX-001 appartient au seed.
        var request = new { Sku = "KB-MX-001", Name = "Doublon fictif", Price = 1m, AvailableStock = 1, CategoryId = PeripheralsCategoryId };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_UnknownCategory_ReturnsBadRequest()
    {
        var request = new { Sku = UniqueSku(), Name = "Produit fictif", Price = 1m, AvailableStock = 1, CategoryId = 999 };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Category 999 does not exist.", await response.Content.ReadFromJsonAsync<string>());
    }

    [Fact]
    public async Task GetCategories_SeededCatalog_ReturnsCategoriesSortedByName()
    {
        var response = await _client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.Equal(["Accessories", "Displays", "Peripherals"], categories!.Select(c => c.Name));
    }

    [Fact]
    public async Task GetProducts_CategoryFilter_ReturnsOnlyProductsOfThatCategory()
    {
        var response = await _client.GetAsync($"/api/products?categoryId={PeripheralsCategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        Assert.All(products, p => Assert.Equal(PeripheralsCategoryId, p.CategoryId));
        Assert.Superset(new HashSet<string> { "KB-MX-001", "MS-WL-002" }, products.Select(p => p.Sku).ToHashSet());
    }

    [Fact]
    public async Task GetProducts_SearchFilter_MatchesNameCaseInsensitively()
    {
        var response = await _client.GetAsync("/api/products?search=MONITOR");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name == "4K Monitor");
        Assert.All(products, p => Assert.Contains("monitor", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Health_DevelopmentEnvironment_ReturnsHealthy()
    {
        // /health n'est exposé qu'en Development, l'environnement par défaut de WebApplicationFactory.
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // Identifiants des catégories du seed (CatalogDbContext).
    private const int PeripheralsCategoryId = 1;
    private const int AccessoriesCategoryId = 3;

    // Le SKU est unique en base : chaque test qui crée un produit utilise le sien.
    private static string UniqueSku() => $"TST-{Guid.NewGuid():N}";

    private sealed record ProductResponse(int Id, string Sku, string Name, string? Description, decimal Price, int AvailableStock, int CategoryId);
    private sealed record CategoryResponse(int Id, string Name);
}
