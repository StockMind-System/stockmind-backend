using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using StockMind.Infrastructure.Persistence;
using StockMind.Reporting.Application;
using StockMind.Reporting.Domain;

namespace StockMind.Reporting.Infrastructure.Persistence;

internal sealed class MongoReportReader(IMongoDatabase database) : IReportReader
{
    public async Task<ReportSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var productCount = await database.GetCollection<BsonDocument>("productos")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty, cancellationToken: ct);
        var sales = await database.GetCollection<BsonDocument>("ventas")
            .Find(Builders<BsonDocument>.Filter.Empty).ToListAsync(ct);
        return new ReportSummary(productCount, sales.Count, sales.Sum(ReadTotal));
    }

    private static decimal ReadTotal(BsonDocument sale)
    {
        if (!sale.TryGetValue("Detalles", out var details) || !details.IsBsonArray) return 0;
        return details.AsBsonArray.Sum(item =>
        {
            var detail = item.AsBsonDocument;
            return detail.GetValue("Cantidad", 0).ToInt32()
                * detail.GetValue("PrecioUnitario", 0).ToDecimal();
        });
    }
}
public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services) =>
        services.AddScoped<IReportReader, MongoReportReader>();
}

