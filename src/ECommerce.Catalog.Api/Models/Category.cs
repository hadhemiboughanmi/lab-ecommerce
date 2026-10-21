using System.Text.Json.Serialization;

namespace ECommerce.Catalog.Api.Models;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Not serialized: listing categories must not pull the whole catalog.
    [JsonIgnore]
    public List<Product> Products { get; set; } = [];
}
