using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StockMind.Infrastructure.Persistence;
using StockMind.Inventory.Application;
using StockMind.Inventory.Domain.Entities;

namespace StockMind.Inventory.Infrastructure.Persistence;

internal sealed class InventoryRepository(IMongoDatabase database) : IInventoryRepository
{
    private IMongoCollection<InventoryMovement> Movements =>
        database.GetCollection<InventoryMovement>("movimientos_inventario");

    public async Task<IReadOnlyCollection<InventoryMovement>> ListAsync(CancellationToken ct = default) =>
        await Movements.Find(Builders<InventoryMovement>.Filter.Empty).ToListAsync(ct);
    public Task CreateAsync(InventoryMovement movement, CancellationToken ct = default) =>
        Movements.InsertOneAsync(movement, cancellationToken: ct);
}

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services) =>
        services.AddScoped<IInventoryRepository, InventoryRepository>();
}

