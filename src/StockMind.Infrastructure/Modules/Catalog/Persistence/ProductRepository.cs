using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StockMind.Catalog.Application;
using StockMind.Catalog.Domain.Entities;
using StockMind.Infrastructure.Persistence;

namespace StockMind.Catalog.Infrastructure.Persistence;

internal sealed class ProductRepository(IMongoDatabase database) : IProductRepository
{
    private IMongoCollection<Product> Products => database.GetCollection<Product>("productos");

    public async Task<IReadOnlyCollection<Product>> ListAsync(CancellationToken ct = default) =>
        await Products.Find(x => x.Activo).ToListAsync(ct);
    public async Task<Product?> GetAsync(string id, CancellationToken ct = default) =>
        await Products.Find(x => x.Id == id && x.Activo).FirstOrDefaultAsync(ct);
    public Task CreateAsync(Product product, CancellationToken ct = default) =>
        Products.InsertOneAsync(product, cancellationToken: ct);
    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        Products.UpdateOneAsync(
            x => x.Id == id && x.Activo,
            Builders<Product>.Update.Set(x => x.Activo, false),
            cancellationToken: ct);
}

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services) =>
        services.AddScoped<IProductRepository, ProductRepository>();
}

