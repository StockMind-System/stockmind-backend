using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StockMind.Infrastructure.Persistence;
using StockMind.Sales.Application;
using StockMind.Sales.Domain.Entities;

namespace StockMind.Sales.Infrastructure.Persistence;

internal sealed class SaleRepository(IMongoDatabase database) : ISaleRepository
{
    private IMongoCollection<Sale> Sales => database.GetCollection<Sale>("ventas");
    public async Task<IReadOnlyCollection<Sale>> ListAsync(CancellationToken ct = default) =>
        await Sales.Find(Builders<Sale>.Filter.Empty).ToListAsync(ct);
    public Task CreateAsync(Sale sale, CancellationToken ct = default) =>
        Sales.InsertOneAsync(sale, cancellationToken: ct);
}

public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services) =>
        services.AddScoped<ISaleRepository, SaleRepository>();
}

