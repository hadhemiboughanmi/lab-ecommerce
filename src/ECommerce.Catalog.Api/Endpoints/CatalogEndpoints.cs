using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ECommerce.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products").WithTags("Catalog");

        // Optional filters: ?categoryId=2 and/or ?search=mouse (case-insensitive, on the name).
        group.MapGet("/", async (int? categoryId, string? search, CatalogDbContext db) =>
        {
            var query = db.Products.AsNoTracking();

            if (categoryId is not null)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search.Trim()}%"));
            }

            return await query.OrderBy(p => p.Name).ToListAsync();
        })
        .WithName("GetProducts");

        group.MapGet("/{id:int}", async (int id, CatalogDbContext db) =>
            await db.Products.FindAsync(id) is { } product
                ? Results.Ok(product)
                : Results.NotFound())
            .WithName("GetProductById");

        group.MapPost("/", async (CreateProductRequest request, CatalogDbContext db) =>
        {
            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                return Results.BadRequest($"Category {request.CategoryId} does not exist.");
            }

            if (await db.Products.AnyAsync(p => p.Sku == request.Sku))
            {
                return Results.Conflict($"A product with SKU '{request.Sku}' already exists.");
            }

            var product = new Product
            {
                Sku = request.Sku,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                AvailableStock = request.AvailableStock,
                CategoryId = request.CategoryId
            };

            db.Products.Add(product);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two concurrent requests passed the check above: the unique index decides.
                return Results.Conflict($"A product with SKU '{request.Sku}' already exists.");
            }

            return Results.CreatedAtRoute("GetProductById", new { id = product.Id }, product);
        })
        .WithName("CreateProduct");

        routes.MapGet("/api/categories", async (CatalogDbContext db) =>
            await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync())
            .WithTags("Catalog")
            .WithName("GetCategories");

        return group;
    }
}

public record CreateProductRequest(
    string Sku, string Name, string? Description, decimal Price, int AvailableStock, int CategoryId);
