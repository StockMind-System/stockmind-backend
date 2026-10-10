using StockMind.Alerts.Domain.Entities;

namespace StockMind.Alerts.Application;

public sealed record ProductStock(string Id, int Current, int Minimum);
public sealed record ReplenishmentAlertResponse(
    string Id, string ProductId, int SuggestedQuantity, string Status, DateTime CreatedAtUtc);
public interface IProductStockReader
{
    Task<IReadOnlyCollection<ProductStock>> ListAsync(CancellationToken cancellationToken = default);
}
public interface IAlertUseCases
{
    Task<IReadOnlyCollection<ReplenishmentAlertResponse>> ListAsync(CancellationToken cancellationToken = default);
}
public sealed class AlertUseCases(IProductStockReader products) : IAlertUseCases
{
    public async Task<IReadOnlyCollection<ReplenishmentAlertResponse>> ListAsync(CancellationToken ct = default) =>
        (await products.ListAsync(ct))
            .Where(x => x.Current <= x.Minimum)
            .Select(x => new ReplenishmentAlert
            {
                ProductoId = x.Id,
                CantidadSugerida = Math.Max(x.Minimum * 2 - x.Current, 1)
            })
            .Select(x => new ReplenishmentAlertResponse(
                x.Id, x.ProductoId, x.CantidadSugerida, x.Estado.ToString(), x.FechaUtc))
            .ToArray();
}

