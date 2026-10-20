using System.Text.Json.Serialization;

namespace ECommerce.Catalog.Api.Models;

public class Product
{
    public int Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }

    public int CategoryId { get; set; }

    // The API exposes CategoryId only.
    [JsonIgnore]
    public Category? Category { get; set; }
}
