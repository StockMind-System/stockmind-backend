using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StockMind.Catalog.Domain.Entities;
using StockMind.Infrastructure.Persistence;
using StockMind.Inventory.Domain.Entities;
using StockMind.Sales.Application;
using StockMind.Sales.Domain.Entities;

namespace StockMind.Sales.Infrastructure.Persistence;

internal sealed class SaleRepository(IMongoDatabase database) : ISaleRepository
{
    private IMongoCollection<Sale> Sales => database.GetCollection<Sale>("ventas");
    private IMongoCollection<Product> Products => database.GetCollection<Product>("productos");
    private IMongoCollection<InventoryMovement> Movements =>
        database.GetCollection<InventoryMovement>("movimientos_inventario");

    public async Task<IReadOnlyCollection<Sale>> ListAsync(CancellationToken ct = default) =>
        await Sales.Find(Builders<Sale>.Filter.Empty).ToListAsync(ct);

    public async Task CreateAsync(Sale sale, CancellationToken ct = default)
    {
        var reserved = new List<(string ProductId, int Quantity)>();
        var saleInserted = false;

        try
        {
            foreach (var group in sale.Detalles.GroupBy(x => x.ProductoId))
            {
                var quantity = group.Sum(x => x.Cantidad);
                var filter = Builders<Product>.Filter.Eq(x => x.Id, group.Key) &
                             Builders<Product>.Filter.Eq(x => x.Activo, true) &
                             Builders<Product>.Filter.Gte(x => x.StockActual, quantity);
                var product = await Products.FindOneAndUpdateAsync(
                    filter,
                    Builders<Product>.Update.Inc(x => x.StockActual, -quantity),
                    new FindOneAndUpdateOptions<Product> { ReturnDocument = ReturnDocument.Before },
                    ct);
                if (product is null)
                    throw new ArgumentException(
                        $"El producto {group.Key} no existe o no tiene stock suficiente.",
                        nameof(sale));

                foreach (var detail in group)
                    detail.PrecioUnitario = product.Precio;
                reserved.Add((group.Key, quantity));
            }

            await Sales.InsertOneAsync(sale, cancellationToken: ct);
            saleInserted = true;
            var movements = reserved.Select(item => new InventoryMovement
            {
                ProductoId = item.ProductId,
                Tipo = MovementType.Salida,
                Cantidad = item.Quantity,
                Motivo = $"Venta {sale.Id}",
                FechaUtc = sale.FechaUtc
            });
            await Movements.InsertManyAsync(movements, cancellationToken: ct);
        }
        catch
        {
            if (saleInserted)
                await Sales.DeleteOneAsync(x => x.Id == sale.Id, CancellationToken.None);
            await Movements.DeleteManyAsync(
                x => x.Motivo == $"Venta {sale.Id}",
                CancellationToken.None);
            foreach (var item in reserved)
            {
                await Products.UpdateOneAsync(
                    x => x.Id == item.ProductId,
                    Builders<Product>.Update.Inc(x => x.StockActual, item.Quantity),
                    cancellationToken: CancellationToken.None);
            }
            throw;
        }
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services) =>
        services.AddScoped<ISaleRepository, SaleRepository>();
}

