using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Catalog.Api.Data;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time, to build the model outside Aspire.
/// At runtime the connection string comes from Aspire (resource "catalogdb").
/// <c>migrations add</c> never opens a connection, so this string carries no credentials;
/// <c>database update</c> needs one: pass it with <c>--connection</c>, never write it here.
/// </summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=catalogdb")
            .Options;

        return new CatalogDbContext(options);
    }
}
