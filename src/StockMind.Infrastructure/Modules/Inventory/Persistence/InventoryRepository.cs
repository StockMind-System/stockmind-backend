using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StockMind.Catalog.Domain.Entities;
using StockMind.Infrastructure.Persistence;
using StockMind.Inventory.Application;
using StockMind.Inventory.Domain.Entities;

namespace StockMind.Inventory.Infrastructure.Persistence;

internal sealed class InventoryRepository(IMongoDatabase database) : IInventoryRepository
{
    private IMongoCollection<InventoryMovement> Movements =>
        database.GetCollection<InventoryMovement>("movimientos_inventario");
    private IMongoCollection<Product> Products => database.GetCollection<Product>("productos");

    public async Task<IReadOnlyCollection<InventoryMovement>> ListAsync(CancellationToken ct = default) =>
        await Movements.Find(Builders<InventoryMovement>.Filter.Empty).ToListAsync(ct);
    public async Task CreateAsync(InventoryMovement movement, CancellationToken ct = default)
    {
        var productFilter = Builders<Product>.Filter.Eq(x => x.Id, movement.ProductoId) &
                            Builders<Product>.Filter.Eq(x => x.Activo, true);
        UpdateDefinition<Product> update;

        switch (movement.Tipo)
        {
            case MovementType.Entrada:
                update = Builders<Product>.Update.Inc(x => x.StockActual, movement.Cantidad);
                break;
            case MovementType.Salida:
                productFilter &= Builders<Product>.Filter.Gte(x => x.StockActual, movement.Cantidad);
                update = Builders<Product>.Update.Inc(x => x.StockActual, -movement.Cantidad);
                break;
            case MovementType.Ajuste:
                update = Builders<Product>.Update.Set(x => x.StockActual, movement.Cantidad);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(movement), "El tipo de movimiento no es valido.");
        }

        var previous = await Products.FindOneAndUpdateAsync(
            productFilter,
            update,
            new FindOneAndUpdateOptions<Product> { ReturnDocument = ReturnDocument.Before },
            ct);
        if (previous is null)
            throw new ArgumentException("El producto no existe o no tiene stock suficiente.", nameof(movement));

        try
        {
            await Movements.InsertOneAsync(movement, cancellationToken: ct);
        }
        catch
        {
            var rollback = movement.Tipo switch
            {
                MovementType.Entrada => Builders<Product>.Update.Inc(x => x.StockActual, -movement.Cantidad),
                MovementType.Salida => Builders<Product>.Update.Inc(x => x.StockActual, movement.Cantidad),
                _ => Builders<Product>.Update.Set(x => x.StockActual, previous.StockActual)
            };
            var rollbackFilter = Builders<Product>.Filter.Eq(x => x.Id, previous.Id);
            if (movement.Tipo == MovementType.Ajuste)
                rollbackFilter &= Builders<Product>.Filter.Eq(x => x.StockActual, movement.Cantidad);
            await Products.UpdateOneAsync(
                rollbackFilter,
                rollback,
                cancellationToken: CancellationToken.None);
            throw;
        }
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services) =>
        services.AddScoped<IInventoryRepository, InventoryRepository>();
}

