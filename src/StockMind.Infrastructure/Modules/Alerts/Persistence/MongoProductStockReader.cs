using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using StockMind.Alerts.Application;
using StockMind.Infrastructure.Persistence;

namespace StockMind.Alerts.Infrastructure.Persistence;

internal sealed class MongoProductStockReader(IMongoDatabase database) : IProductStockReader
{
    public async Task<IReadOnlyCollection<ProductStock>> ListAsync(CancellationToken ct = default)
    {
        var documents = await database.GetCollection<BsonDocument>("productos")
            .Find(Builders<BsonDocument>.Filter.Empty).ToListAsync(ct);
        return documents.Select(x => new ProductStock(
            x.GetValue("Id", x.GetValue("_id", "")).ToString() ?? string.Empty,
            x.GetValue("StockActual", 0).ToInt32(),
            x.GetValue("StockMinimo", 0).ToInt32())).ToArray();
    }
}
public static class DependencyInjection
{
    public static IServiceCollection AddAlertsInfrastructure(this IServiceCollection services) =>
        services.AddScoped<IProductStockReader, MongoProductStockReader>();
}

