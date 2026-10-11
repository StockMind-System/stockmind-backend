using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StockMind.Alerts.Application;
using StockMind.Catalog.Domain.Entities;
using StockMind.Infrastructure.Persistence;

namespace StockMind.Alerts.Infrastructure.Persistence;

internal sealed class MongoProductStockReader(IMongoDatabase database) : IProductStockReader
{
    public async Task<IReadOnlyCollection<ProductStock>> ListAsync(CancellationToken ct = default)
    {
        var products = await database.GetCollection<Product>("productos")
            .Find(product => product.Activo)
            .ToListAsync(ct);
        return products.Select(product => new ProductStock(
            product.Id,
            product.StockActual,
            product.StockMinimo)).ToArray();
    }
}
public static class DependencyInjection
{
    public static IServiceCollection AddAlertsInfrastructure(this IServiceCollection services) =>
        services.AddScoped<IProductStockReader, MongoProductStockReader>();
}

