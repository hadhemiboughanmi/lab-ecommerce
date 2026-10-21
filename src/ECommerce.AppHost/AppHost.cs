var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL server (Docker container) + the Catalog database.
// - Fixed host port 5433: tools outside Aspire (psql, MCP servers) always find it.
// - Data volume: data survives an AppHost restart.
// - The password is an Aspire parameter, generated and kept in the AppHost user secrets.
var postgres = builder.AddPostgres("postgres", port: 5433)
    .WithDataVolume("ecommerce-pgdata");

var catalogDb = postgres.AddDatabase("catalogdb");

// Catalog microservice — backed by PostgreSQL.
var catalog = builder.AddProject<Projects.ECommerce_Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

// Ordering microservice — depends on Catalog (resolved via service discovery).
var ordering = builder.AddProject<Projects.ECommerce_Ordering_Api>("ordering")
    .WithReference(catalog)
    .WaitFor(catalog);

// API Gateway (YARP) — the single entry point for the services.
var gateway = builder.AddProject<Projects.ECommerce_Gateway>("gateway")
    .WithReference(catalog)
    .WithReference(ordering)
    .WaitFor(catalog)
    .WaitFor(ordering);

// Blazor web frontend — talks to the services through the gateway.
builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();
