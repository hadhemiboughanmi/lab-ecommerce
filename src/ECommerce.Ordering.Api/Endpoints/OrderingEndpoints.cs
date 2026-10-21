using System.Diagnostics;
using ECommerce.Ordering.Api.Data;
using ECommerce.Ordering.Api.Diagnostics;
using ECommerce.Ordering.Api.Models;
using ECommerce.Ordering.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Api.Endpoints;

public static class OrderingEndpoints
{
    public static RouteGroupBuilder MapOrderingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/orders").WithTags("Ordering");

        group.MapGet("/", async (OrderingDbContext db) =>
            await db.Orders.AsNoTracking().Include(o => o.Items).ToListAsync())
            .WithName("GetOrders");

        group.MapGet("/{id:int}", async (int id, OrderingDbContext db) =>
            await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id) is { } order
                ? Results.Ok(order)
                : Results.NotFound())
            .WithName("GetOrderById");

        group.MapPost("/", async (
            CreateOrderRequest request,
            OrderingDbContext db,
            CatalogServiceClient catalog,
            OrderingTelemetry telemetry,
            ILogger<Order> logger,
            CancellationToken ct) =>
        {
            if (request.Items.Count == 0)
            {
                telemetry.Reject("empty_order");
                logger.LogWarning("Order rejected: no item");
                return Results.BadRequest("An order must contain at least one item.");
            }

            var invalidLine = request.Items.FirstOrDefault(i => i.Quantity <= 0);
            if (invalidLine is not null)
            {
                telemetry.Reject("invalid_quantity");
                logger.LogWarning("Order rejected: quantity {Quantity} for product {ProductId}", invalidLine.Quantity, invalidLine.ProductId);
                return Results.BadRequest($"Quantity for product {invalidLine.ProductId} must be greater than 0.");
            }

            var order = new Order
            {
                Customer = request.Customer,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OrderStatus.Pending
            };

            // Child span: makes the sequential calls to Catalog (one per line) visible in the trace.
            using (var validation = OrderingTelemetry.ActivitySource.StartActivity("ValidateOrderItems"))
            {
                validation?.SetTag("order.items.count", request.Items.Count);

                foreach (var item in request.Items)
                {
                    var product = await catalog.GetProductAsync(item.ProductId, ct);
                    if (product is null)
                    {
                        validation?.SetStatus(ActivityStatusCode.Error, "unknown product");
                        telemetry.Reject("unknown_product");
                        logger.LogWarning("Order rejected: product {ProductId} does not exist", item.ProductId);
                        return Results.BadRequest($"Product {item.ProductId} does not exist.");
                    }

                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = item.Quantity
                    });
                }
            }

            order.Status = OrderStatus.Confirmed;
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);

            telemetry.OrdersCreated.Add(1);
            telemetry.OrderAmount.Record((double)order.Total);

            // Identifiers and amounts only, never the customer: telemetry is copied to the
            // dashboard, collectors, tickets and agents.
            logger.LogInformation(
                "Order {OrderId} confirmed: {ItemCount} line(s), total {OrderTotal}",
                order.Id, order.Items.Count, order.Total);

            return Results.CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
        })
        .WithName("CreateOrder");

        return group;
    }
}

public record CreateOrderRequest(string Customer, List<OrderLine> Items);
public record OrderLine(int ProductId, int Quantity);
