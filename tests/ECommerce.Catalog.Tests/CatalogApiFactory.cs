using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace ECommerce.Catalog.Tests;

/// <summary>
/// Démarre l'API Catalog en mémoire, branchée sur un vrai PostgreSQL jetable (Testcontainers) :
/// le provider in-memory ignorait les CHECK, l'index unique sur le SKU et les séquences.
/// Au démarrage (environnement Development), Program.cs applique les migrations.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Image épinglée ; identifiants générés par Testcontainers pour un conteneur local détruit
    // en fin de suite : pas un secret.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("catalogdb")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    // Remplace la chaîne de connexion qu'Aspire injecterait.
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:catalogdb", _postgres.GetConnectionString());

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
