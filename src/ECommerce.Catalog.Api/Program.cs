using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire: telemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// PostgreSQL through Aspire: the "catalogdb" connection string is injected by the AppHost.
// Also adds a database health check, EF Core/Npgsql traces and metrics, and retries.
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

var app = builder.Build();

// Aspire default endpoints (/health, /alive).
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Development only: apply pending migrations at startup. In shared environments,
    // migrations are a reviewed, explicit step, never applied by the service itself.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();
}

app.MapCatalogEndpoints();

app.Run();
